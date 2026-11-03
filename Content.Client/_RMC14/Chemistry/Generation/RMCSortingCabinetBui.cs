using System.Linq;
using Content.Shared._RMC14.Chemistry.Generation;
using Content.Shared._RMC14.Chemistry.Reagent;
using Content.Shared._RMC14.UserInterface;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client._RMC14.Chemistry.Generation;

[UsedImplicitly]
public sealed class RMCSortingCabinetBui : BoundUserInterface, IRefreshableBui
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    private const string HighlightColor = "#FFD700";

    private readonly SharedContainerSystem _container;
    private readonly RMCReagentSystem _rmcReagent;

    private RMCSortingCabinetWindow? _window;
    private SortMode _sort = SortMode.None;
    private bool _descending = true;
    private string _search = string.Empty;
    private readonly HashSet<EntityUid> _expanded = new();

    private enum SortMode
    {
        None,
        Value,
        Overdose,
        Class,
    }

    private readonly record struct Entry(
        EntityUid Document,
        string Name,
        int Overdose,
        float Value,
        int Type,
        List<(string Code, int Level)> Properties);

    public RMCSortingCabinetBui(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        IoCManager.InjectDependencies(this);
        _container = EntMan.System<SharedContainerSystem>();
        _rmcReagent = EntMan.System<RMCReagentSystem>();
    }

    protected override void Open()
    {
        base.Open();
        _window = this.CreateWindow<RMCSortingCabinetWindow>();
        _window.Title = EntMan.GetComponent<MetaDataComponent>(Owner).EntityName;

        _window.SearchEdit.OnTextChanged += args =>
        {
            _search = args.Text.ToUpperInvariant();
            Refresh();
        };

        _window.SortValueButton.OnPressed += _ => SetSort(SortMode.Value);
        _window.SortOverdoseButton.OnPressed += _ => SetSort(SortMode.Overdose);
        _window.SortClassButton.OnPressed += _ => SetSort(SortMode.Class);
        _window.OrderButton.OnPressed += _ =>
        {
            _descending = !_descending;
            Refresh();
        };

        Refresh();
    }

    private void SetSort(SortMode sort)
    {
        _sort = sort;
        Refresh();
    }

    public void Refresh()
    {
        if (_window is not { IsOpen: true } ||
            !EntMan.TryGetComponent(Owner, out RMCSortingCabinetComponent? cabinet))
        {
            return;
        }

        _window.SetSortButton(_window.SortValueButton, _sort == SortMode.Value);
        _window.SetSortButton(_window.SortOverdoseButton, _sort == SortMode.Overdose);
        _window.SetSortButton(_window.SortClassButton, _sort == SortMode.Class);
        _window.SetDescending(_descending);

        var entries = new List<Entry>();
        if (_container.TryGetContainer(Owner, cabinet.ContainerId, out var container))
        {
            foreach (var document in container.ContainedEntities)
            {
                if (TryGetEntry(document, out var entry))
                    entries.Add(entry);
            }
        }

        _expanded.RemoveWhere(e => entries.All(entry => entry.Document != e));
        _window.EmptyPanel.Visible = entries.Count == 0;

        IEnumerable<Entry> sorted = _sort switch
        {
            SortMode.Value => entries.OrderBy(e => e.Value),
            SortMode.Overdose => entries.OrderBy(e => e.Overdose),
            _ => entries.OrderBy(e => e.Type),
        };

        var compact = _search.Replace(" ", string.Empty);
        if (_search.Length > 0)
            sorted = sorted.Where(e => Matches(e, compact));

        if (_descending)
            sorted = sorted.Reverse();

        var highlight = _search.Length >= 3 || StartsWithNumber(_search);

        _window.Documents.RemoveAllChildren();
        foreach (var entry in sorted)
        {
            var row = new RMCSortingCabinetRow();
            var document = entry.Document;

            var title = new FormattedMessage();
            title.AddText(entry.Name);
            for (var i = 0; i < entry.Properties.Count; i++)
            {
                var (code, level) = entry.Properties[i];
                title.AddText(i == 0 ? " " : ", ");

                if (highlight && PropertyMatches(code, level, compact))
                {
                    title.PushColor(Color.FromHex(HighlightColor));
                    title.AddText($"{code}{level}");
                    title.Pop();
                }
                else
                {
                    title.AddText($"{code}{level}");
                }
            }

            row.TitleLabel.SetMessage(title);
            row.OverdoseLabel.Text = $"Overdose: {entry.Overdose}";
            row.ValueLabel.Text = $"Total Value: {entry.Value:0.##}";
            row.SetExpanded(_expanded.Contains(document));

            row.ExpandButton.OnToggled += args =>
            {
                if (args.Pressed)
                    _expanded.Add(document);
                else
                    _expanded.Remove(document);

                row.SetExpanded(args.Pressed);
            };

            row.TakeOutButton.OnPressed += _ =>
                SendPredictedMessage(new RMCSortingCabinetTakeOutBuiMsg(EntMan.GetNetEntity(document)));

            _window.Documents.AddChild(row);
        }
    }

    private bool TryGetEntry(EntityUid document, out Entry entry)
    {
        entry = default;
        if (!EntMan.TryGetComponent(document, out RMCChemResearchReportComponent? report))
            return false;

        var value = 0f;
        var properties = new List<(string Code, int Level)>();
        if (report.Completed)
        {
            foreach (var property in report.Properties)
            {
                if (!_prototype.TryIndex<ChemGeneratorPropertyPrototype>(property.PropertyId, out var proto))
                    continue;

                var attitude = proto.Category switch
                {
                    ChemPropertyCategory.Negative => -1.5f,
                    ChemPropertyCategory.Neutral => -1f,
                    _ => 1f,
                };

                value += property.Level * attitude;
                properties.Add((string.IsNullOrEmpty(proto.Code) ? proto.ID : proto.Code, property.Level));
            }
        }

        value += report.Overdose / 5f;

        var documentName = EntMan.GetComponent<MetaDataComponent>(document).EntityName;
        var name = report.ReagentId is { } reagentId
            ? _rmcReagent.TryIndex(reagentId, out var reagent) ? reagent.LocalizedName : reagentId.Id
            : documentName;

        var type = documentName.Split(' ')[0] switch
        {
            "Contract" => 1,
            "Synthesis" => 2,
            _ => 3,
        };

        entry = new Entry(document, name, report.Overdose, value, type, properties);
        return true;
    }

    private bool Matches(Entry entry, string compact)
    {
        return entry.Properties.Any(p => PropertyMatches(p.Code, p.Level, compact)) ||
               entry.Name.ToUpperInvariant().Contains(_search) ||
               entry.Overdose.ToString() == _search;
    }

    private bool PropertyMatches(string code, int level, string compact)
    {
        var upper = code.ToUpperInvariant();
        return upper.Contains(compact) ||
               level.ToString() == _search ||
               $"{upper}{level}" == compact;
    }

    private static bool StartsWithNumber(string text)
    {
        var span = text.AsSpan().TrimStart();
        if (span.Length > 0 && span[0] is '+' or '-')
            span = span[1..];

        if (span.Length > 0 && span[0] == '.')
            span = span[1..];

        return span.Length > 0 && char.IsAsciiDigit(span[0]);
    }
}
