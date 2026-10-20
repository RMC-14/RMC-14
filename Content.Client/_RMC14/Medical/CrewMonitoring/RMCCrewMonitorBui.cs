using System.Linq;
using Content.Client._RMC14.UserInterface;
using Content.Shared._RMC14.Medical.CrewMonitoring;
using Content.Shared.Mobs;
using Content.Shared.Roles;
using Content.Shared.StatusIcon;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;

namespace Content.Client._RMC14.Medical.CrewMonitoring;

public sealed class RMCCrewMonitorBui : RMCPopOutBui<RMCCrewMonitorWindow>
{
    private readonly IPrototypeManager _prototypes;
    private readonly SpriteSystem _sprite;

    private CrewMonitorStatusFilter _statusFilter;
    private CrewMonitorLocationFilter _locationFilter;

    protected override RMCCrewMonitorWindow? Window { get; set; }

    public RMCCrewMonitorBui(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        _prototypes = IoCManager.Resolve<IPrototypeManager>();
        _sprite = EntMan.System<SpriteSystem>();
    }

    protected override void Open()
    {
        base.Open();
        Window = this.CreatePopOutableWindow<RMCCrewMonitorWindow>();

        Window.Search.OnTextChanged += _ => Refresh();
        Window.RefreshButton.OnPressed += _ => SendMessage(new RMCCrewMonitorRefreshBuiMsg());
        PopulateFilters();
        Refresh();
    }

    public void Refresh()
    {
        if (Window == null || !EntMan.TryGetComponent(Owner, out RMCCrewMonitorComponent? monitor))
            return;

        UpdateCounters(monitor.Entries);

        var openGroups = new Dictionary<string, bool>();
        foreach (var child in Window.Groups.Children)
        {
            if (child is RMCCrewMonitorGroup group)
                openGroups[group.GroupKey] = group.Collapsible.BodyVisible;
        }

        Window.Groups.RemoveAllChildren();
        var filtered = monitor.Entries.Where(MatchesFilters).ToList();
        var groups = GroupEntries(filtered);
        foreach (var groupData in groups)
        {
            var group = new RMCCrewMonitorGroup
            {
                GroupKey = groupData.Key,
            };
            group.Heading.Title = Loc.GetString(
                "rmc-crew-monitor-group-title",
                ("name", groupData.Name),
                ("count", groupData.Entries.Count));
            group.Heading.Modulate = groupData.Color;
            if (openGroups.TryGetValue(groupData.Key, out var wasOpen))
                group.Collapsible.BodyVisible = wasOpen;

            foreach (var entry in groupData.Entries)
                group.Rows.AddChild(CreateRow(entry));

            Window.Groups.AddChild(group);
        }

        Window.Empty.Visible = groups.Count == 0;
        Window.TableHeader.Visible = groups.Count > 0;
        Window.ResultsScroll.Visible = groups.Count > 0;
    }

    private void PopulateFilters()
    {
        if (Window == null)
            return;

        Window.StatusFilter.AddItem(Loc.GetString("rmc-crew-monitor-filter-all"), (int) CrewMonitorStatusFilter.All);
        Window.StatusFilter.AddItem(Loc.GetString("rmc-crew-monitor-status-alive"), (int) CrewMonitorStatusFilter.Alive);
        Window.StatusFilter.AddItem(Loc.GetString("rmc-crew-monitor-status-critical"), (int) CrewMonitorStatusFilter.Critical);
        Window.StatusFilter.AddItem(Loc.GetString("rmc-crew-monitor-status-dead"), (int) CrewMonitorStatusFilter.Dead);
        Window.StatusFilter.SelectId((int) CrewMonitorStatusFilter.All);
        Window.StatusFilter.OnItemSelected += args =>
        {
            Window.StatusFilter.SelectId(args.Id);
            _statusFilter = (CrewMonitorStatusFilter) args.Id;
            Refresh();
        };

        Window.LocationFilter.AddItem(Loc.GetString("rmc-crew-monitor-filter-all"), (int) CrewMonitorLocationFilter.All);
        Window.LocationFilter.AddItem(Loc.GetString("rmc-crew-monitor-location-ship"), (int) CrewMonitorLocationFilter.Ship);
        Window.LocationFilter.AddItem(Loc.GetString("rmc-crew-monitor-location-planet"), (int) CrewMonitorLocationFilter.Planet);
        Window.LocationFilter.SelectId((int) CrewMonitorLocationFilter.All);
        Window.LocationFilter.OnItemSelected += args =>
        {
            Window.LocationFilter.SelectId(args.Id);
            _locationFilter = (CrewMonitorLocationFilter) args.Id;
            Refresh();
        };
    }

    private bool MatchesFilters(RMCCrewMonitorEntry entry)
    {
        if (Window == null)
            return false;

        if (_statusFilter != CrewMonitorStatusFilter.All &&
            (int) _statusFilter != (int) entry.State)
        {
            return false;
        }

        if (_locationFilter != CrewMonitorLocationFilter.All &&
            entry.Location != (RMCCrewMonitorLocation?) ((int) _locationFilter - 1))
        {
            return false;
        }

        var search = Window.Search.Text.Trim();
        return search.Length == 0 ||
               entry.Name.Contains(search, StringComparison.CurrentCultureIgnoreCase) ||
               entry.JobTitle.Contains(search, StringComparison.CurrentCultureIgnoreCase) ||
               entry.AreaName?.Contains(search, StringComparison.CurrentCultureIgnoreCase) == true ||
               entry.Squad?.Contains(search, StringComparison.CurrentCultureIgnoreCase) == true;
    }

    private List<CrewMonitorGroupData> GroupEntries(List<RMCCrewMonitorEntry> entries)
    {
        var groups = new Dictionary<string, CrewMonitorGroupData>();
        foreach (var entry in entries)
        {
            var data = GetGroup(entry);
            if (!groups.TryGetValue(data.Key, out var group))
            {
                group = data;
                groups.Add(data.Key, group);
            }

            group.Entries.Add(entry);
        }

        var result = groups.Values.OrderByDescending(g => g.Weight).ThenBy(g => g.Name).ToList();
        foreach (var group in result)
        {
            group.Entries.Sort((left, right) => CompareEntries(group, left, right));
        }

        return result;
    }

    private CrewMonitorGroupData GetGroup(RMCCrewMonitorEntry entry)
    {
        if (!string.IsNullOrWhiteSpace(entry.Squad))
        {
            return new CrewMonitorGroupData(
                $"squad:{entry.Squad}",
                entry.Squad,
                entry.SquadColor ?? Color.LightGray,
                -100,
                null);
        }

        DepartmentPrototype? selected = null;
        foreach (var departmentId in entry.Departments)
        {
            if (!_prototypes.TryIndex(departmentId, out var department))
                continue;

            if (selected == null || (!selected.Primary && department.Primary))
                selected = department;
        }

        if (selected != null)
        {
            return new CrewMonitorGroupData(
                $"department:{selected.ID}",
                Loc.GetString(selected.Name),
                selected.Color,
                selected.Weight,
                selected);
        }

        return new CrewMonitorGroupData(
            "unknown",
            Loc.GetString("rmc-crew-monitor-group-unknown"),
            Color.Gray,
            int.MinValue,
            null);
    }

    private int CompareEntries(CrewMonitorGroupData group, RMCCrewMonitorEntry left, RMCCrewMonitorEntry right)
    {
        if (group.Key.StartsWith("squad:", StringComparison.Ordinal))
        {
            var priority = GetRolePriority(left).CompareTo(GetRolePriority(right));
            if (priority != 0)
                return priority;
        }
        else if (group.Department != null)
        {
            var leftIndex = left.Job is { } leftJob ? group.Department.Roles.IndexOf(leftJob) : int.MaxValue;
            var rightIndex = right.Job is { } rightJob ? group.Department.Roles.IndexOf(rightJob) : int.MaxValue;
            if (leftIndex < 0)
                leftIndex = int.MaxValue;
            if (rightIndex < 0)
                rightIndex = int.MaxValue;

            var departmentOrder = leftIndex.CompareTo(rightIndex);
            if (departmentOrder != 0)
                return departmentOrder;
        }

        return string.Compare(left.Name, right.Name, StringComparison.CurrentCultureIgnoreCase);
    }

    private int GetRolePriority(RMCCrewMonitorEntry entry)
    {
        if (entry.Job is not { } role ||
            !_prototypes.TryIndex(role, out var rolePrototype) ||
            rolePrototype.OverwatchSortPriority is not { } priority)
        {
            return int.MaxValue;
        }

        return priority;
    }

    private RMCCrewMonitorRow CreateRow(RMCCrewMonitorEntry entry)
    {
        var row = new RMCCrewMonitorRow();
        row.NameLabel.Text = entry.Name;
        row.JobLabel.Text = entry.JobTitle;

        if (_prototypes.TryIndex(entry.JobIcon, out JobIconPrototype? jobIcon))
            row.JobIcon.Texture = _sprite.Frame0(jobIcon.Icon);

        row.StatusLabel.Text = RMCCrewMonitorUIHelpers.GetStatusName(entry.State);
        row.StatusLabel.FontColorOverride = RMCCrewMonitorUIHelpers.GetStatusColor(entry.State);

        SetDamage(row.BruteLabel, entry.Brute, Color.FromHex("#DF3E3E"));
        SetDamage(row.BurnLabel, entry.Burn, Color.FromHex("#FFB833"));
        SetDamage(row.ToxinLabel, entry.Toxin, Color.FromHex("#25CA4C"));
        SetDamage(row.OxygenLabel, entry.Oxygen, Color.FromHex("#2E93DE"));

        row.LocationLabel.Text = GetLocation(entry);
        row.LocationLabel.FontColorOverride = entry.Location switch
        {
            RMCCrewMonitorLocation.Ship => Color.FromHex("#63B3ED"),
            RMCCrewMonitorLocation.Planet => Color.FromHex("#68D391"),
            _ => Color.Gray,
        };
        return row;
    }

    private static void SetDamage(Label label, float? damage, Color color)
    {
        label.Text = damage == null ? "—" : MathF.Round(damage.Value).ToString("0");
        label.FontColorOverride = damage == null ? Color.Gray : color;
    }

    private string GetLocation(RMCCrewMonitorEntry entry)
    {
        if (entry.Location == null)
            return Loc.GetString("rmc-crew-monitor-location-unavailable");

        var location = entry.Location switch
        {
            RMCCrewMonitorLocation.Ship => Loc.GetString("rmc-crew-monitor-location-ship"),
            RMCCrewMonitorLocation.Planet => Loc.GetString("rmc-crew-monitor-location-planet"),
            _ => Loc.GetString("rmc-crew-monitor-location-unavailable"),
        };
        return string.IsNullOrWhiteSpace(entry.AreaName) ? location : $"{location} · {entry.AreaName}";
    }

    private void UpdateCounters(List<RMCCrewMonitorEntry> entries)
    {
        if (Window == null)
            return;

        Window.TotalCount.Text = Loc.GetString("rmc-crew-monitor-counter-total", ("count", entries.Count));
        Window.AliveCount.Text = Loc.GetString("rmc-crew-monitor-counter-alive", ("count", entries.Count(e => e.State == MobState.Alive)));
        Window.CriticalCount.Text = Loc.GetString("rmc-crew-monitor-counter-critical", ("count", entries.Count(e => e.State == MobState.Critical)));
        Window.DeadCount.Text = Loc.GetString("rmc-crew-monitor-counter-dead", ("count", entries.Count(e => e.State == MobState.Dead)));
    }

    private sealed class CrewMonitorGroupData(
        string key,
        string name,
        Color color,
        int weight,
        DepartmentPrototype? department)
    {
        public readonly Color Color = color;
        public readonly DepartmentPrototype? Department = department;
        public readonly List<RMCCrewMonitorEntry> Entries = new();
        public readonly string Key = key;
        public readonly string Name = name;
        public readonly int Weight = weight;
    }

    private enum CrewMonitorStatusFilter : byte
    {
        All,
        Alive,
        Critical,
        Dead,
    }

    private enum CrewMonitorLocationFilter : byte
    {
        All,
        Ship,
        Planet,
    }
}
