using System.Linq;
using System.Numerics;
using Content.Server.Administration.Logs;
using Content.Server.Administration.Managers;
using Content.Server.Body.Components;
using Content.Server.Roles.Jobs;
using Content.Server.Warps;
using Content.Shared._RMC14.Ghost;
using Content.Shared._RMC14.Marines.Squads;
using Content.Shared._RMC14.Survivor;
using Content.Shared._RMC14.TacticalMap;
using Content.Shared._RMC14.Xenonids;
using Content.Shared.Database;
using Content.Shared.Damage;
using Content.Shared.Follower;
using Content.Shared.Follower.Components;
using Content.Shared.Ghost;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.NPC.Components;
using Content.Shared.NPC.Prototypes;
using Content.Shared.NPC.Systems;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.Warps;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server._RMC14.Ghost;

public sealed class RMCGhostTargetSystem : EntitySystem
{
    private const string MarineFaction = "UNMC";

    private static readonly LocId EmptyTitle = string.Empty;
    private static readonly LocId MarinesTitle = "rmc-ghost-target-window-group-marines";
    private static readonly LocId XenosTitle = "rmc-ghost-target-window-group-xenos";
    private static readonly LocId OthersTitle = "rmc-ghost-target-window-group-others";
    private static readonly LocId DeadsTitle = "rmc-ghost-target-window-group-deads";
    private static readonly LocId GhostsTitle = "rmc-ghost-target-window-group-ghosts";
    private static readonly LocId WarpPointsTitle = "rmc-ghost-target-window-group-warp-points";

    [Dependency] private readonly IAdminLogManager _adminLog = default!;
    [Dependency] private readonly IAdminManager _adminManager = default!;
    [Dependency] private readonly FollowerSystem _followerSystem = default!;
    [Dependency] private readonly JobSystem _jobs = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly MobThresholdSystem _mobThreshold = default!;
    [Dependency] private readonly NpcFactionSystem _npcFaction = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SquadSystem _squad = default!;
    [Dependency] private readonly TransformSystem _transformSystem = default!;

    private EntityQuery<GhostComponent> _ghostQuery;
    private EntityQuery<PhysicsComponent> _physicsQuery;

    public override void Initialize()
    {
        base.Initialize();

        _ghostQuery = GetEntityQuery<GhostComponent>();
        _physicsQuery = GetEntityQuery<PhysicsComponent>();

        SubscribeNetworkEvent<RMCGhostWarpsRequestEvent>(OnGhostWarpsRequest);
        SubscribeNetworkEvent<RMCGhostWarpToTargetRequestEvent>(OnGhostWarpToTargetRequest);
        SubscribeNetworkEvent<RMCGhostnadoRequestEvent>(OnGhostnadoRequest);
    }

    private void OnGhostWarpsRequest(RMCGhostWarpsRequestEvent msg, EntitySessionEventArgs args)
    {
        if (!TryGetSenderGhost(args, out var ghost))
        {
            Log.Warning($"User {args.SenderSession.Name} sent a {nameof(RMCGhostWarpsRequestEvent)} without being a ghost.");
            return;
        }

        var response = new RMCGhostWarpsResponseEvent(BuildSections(ghost, _adminManager.IsAdmin(args.SenderSession)));
        RaiseNetworkEvent(response, args.SenderSession.Channel);
    }

    private void OnGhostWarpToTargetRequest(RMCGhostWarpToTargetRequestEvent msg, EntitySessionEventArgs args)
    {
        if (!TryGetSenderGhost(args, out var ghost))
        {
            Log.Warning($"User {args.SenderSession.Name} tried to RMC ghost warp without being a ghost.");
            return;
        }

        var target = GetEntity(msg.Target);
        if (!Exists(target))
        {
            Log.Warning($"User {args.SenderSession.Name} tried to RMC ghost warp to an invalid entity id: {msg.Target}");
            return;
        }

        WarpTo(ghost, target);
    }

    private void OnGhostnadoRequest(RMCGhostnadoRequestEvent msg, EntitySessionEventArgs args)
    {
        if (!TryGetSenderGhost(args, out var ghost))
        {
            Log.Warning($"User {args.SenderSession.Name} tried to RMC ghostnado without being a ghost.");
            return;
        }

        if (_followerSystem.GetMostGhostFollowed() is not { } target)
            return;

        WarpTo(ghost, target);
    }

    private bool TryGetSenderGhost(EntitySessionEventArgs args, out EntityUid ghost)
    {
        ghost = default;

        if (args.SenderSession.AttachedEntity is not { Valid: true } attached ||
            !_ghostQuery.HasComp(attached))
        {
            return false;
        }

        ghost = attached;
        return true;
    }

    private List<RMCGhostTargetSection> BuildSections(EntityUid ghost, bool showAdminGhosts)
    {
        var factionSections = BuildFactionSections();
        var marines = new SectionBuilder(MarinesTitle, null, Color.FromHex("#1c70b0"));
        var xenos = new SectionBuilder(XenosTitle, null, Color.FromHex("#472f4f"));
        var others = new SectionBuilder(OthersTitle);
        var deads = new SectionBuilder(DeadsTitle, isExpandedByDefault: false);
        var ghosts = new SectionBuilder(GhostsTitle, isExpandedByDefault: false);
        var warpPoints = new SectionBuilder(WarpPointsTitle, isExpandedByDefault: false);

        foreach (var target in GetPlayerTargets(ghost).Concat(GetGhostTargets(ghost, showAdminGhosts)).Concat(GetLocationTargets()))
        {
            var uid = target.Uid;
            var entry = target.Entry;

            if (entry.IsWarpPoint)
            {
                warpPoints.Entries.Add(entry);
                continue;
            }

            if (_ghostQuery.HasComp(uid))
            {
                ghosts.Entries.Add(entry);
                continue;
            }

            if (_mobState.IsDead(uid))
            {
                deads.Entries.Add(entry);
                continue;
            }

            if (TryComp<NpcFactionMemberComponent>(uid, out var factionComp))
            {
                foreach (var faction in factionComp.Factions)
                {
                    if (!factionSections.All.TryGetValue(faction, out var factionSection))
                        continue;

                    factionSection.Entries.Add(entry);
                    goto nextTarget;
                }

                if (_npcFaction.IsMember((uid, factionComp), MarineFaction) &&
                    !HasComp<RMCSurvivorComponent>(uid))
                {
                    AddMarineEntry(marines, uid, entry);
                    continue;
                }
            }

            if (HasComp<XenoComponent>(uid))
            {
                xenos.Entries.Add(entry);
                continue;
            }

            others.Entries.Add(entry);

            nextTarget: ;
        }

        factionSections.Roots.Sort(CompareSectionsByTitle);

        var roots = new List<SectionBuilder> { marines, xenos };
        roots.AddRange(factionSections.Roots);
        roots.Add(others);
        roots.Add(deads);
        roots.Add(warpPoints);
        roots.Add(ghosts);

        foreach (var root in roots)
            SortSection(root);

        roots = roots.Where(HasContent).ToList();
        AssignIndexes(roots);
        return roots.Select(ToSection).ToList();
    }

    private FactionSectionCollection BuildFactionSections()
    {
        var prototypes = new Dictionary<ProtoId<NpcFactionPrototype>, NpcFactionPrototype>();
        foreach (var proto in _prototypes.EnumeratePrototypes<NpcFactionPrototype>())
        {
            if (proto.Name == string.Empty)
                continue;

            prototypes[proto.ID] = proto;
        }

        var all = new Dictionary<ProtoId<NpcFactionPrototype>, SectionBuilder>();
        var childToParent = new Dictionary<ProtoId<NpcFactionPrototype>, ProtoId<NpcFactionPrototype>>();

        foreach (var proto in prototypes.Values)
        {
            BuildFactionSectionTree(proto.ID, prototypes, all, childToParent, new Stack<ProtoId<NpcFactionPrototype>>());
        }

        var roots = new List<SectionBuilder>();
        foreach (var (id, section) in all)
        {
            if (!childToParent.ContainsKey(id))
                roots.Add(section);
        }

        return new FactionSectionCollection(all, roots);
    }

    private SectionBuilder? BuildFactionSectionTree(
        ProtoId<NpcFactionPrototype> id,
        Dictionary<ProtoId<NpcFactionPrototype>, NpcFactionPrototype> prototypes,
        Dictionary<ProtoId<NpcFactionPrototype>, SectionBuilder> all,
        Dictionary<ProtoId<NpcFactionPrototype>, ProtoId<NpcFactionPrototype>> childToParent,
        Stack<ProtoId<NpcFactionPrototype>> path)
    {
        if (path.Contains(id))
        {
            var cycle = string.Join(" -> ", path) + " -> " + id;
            Log.Error($"Cycle detected in RMC ghost target faction groups: {cycle}");
            return null;
        }

        path.Push(id);
        if (!all.TryGetValue(id, out var section))
        {
            if (!prototypes.TryGetValue(id, out var proto))
            {
                path.Pop();
                return null;
            }

            section = new SectionBuilder(proto.Name, null, proto.Color);
            all[id] = section;
        }

        if (prototypes.TryGetValue(id, out var thisProto) &&
            thisProto.Subgroups is { Count: > 0 })
        {
            foreach (var subId in thisProto.Subgroups)
            {
                if (childToParent.ContainsKey(subId))
                    continue;

                var child = BuildFactionSectionTree(subId, prototypes, all, childToParent, path);
                if (child == null)
                    continue;

                section.Children.Add(child);
                childToParent[subId] = id;
            }
        }

        path.Pop();
        return section;
    }

    private void AddMarineEntry(SectionBuilder marines, EntityUid uid, RMCGhostTargetEntry entry)
    {
        if (_squad.TryGetMemberSquad(uid, out var squad))
        {
            var squadName = Name(squad.Owner);
            var squadSection = marines.Children.FirstOrDefault(section => section.Title == squadName);
            if (squadSection == null)
            {
                squadSection = new SectionBuilder(EmptyTitle, squadName, AdjustLightness(squad.Comp.Color, -0.1f));
                marines.Children.Add(squadSection);
            }

            squadSection.Entries.Add(entry);
            return;
        }

        var othersSection = marines.Children.FirstOrDefault(IsOthersSection);
        if (othersSection == null)
        {
            othersSection = new SectionBuilder(OthersTitle);
            marines.Children.Add(othersSection);
        }

        othersSection.Entries.Add(entry);
    }

    private IEnumerable<TargetData> GetLocationTargets()
    {
        var allQuery = AllEntityQuery<WarpPointComponent>();

        while (allQuery.MoveNext(out var uid, out var warp))
        {
            var displayName = warp.Location ?? Name(uid);
            yield return new TargetData(uid, new RMCGhostTargetEntry(
                GetNetEntity(uid),
                displayName,
                displayName,
                null,
                true,
                GetFollowerCount(uid),
                null,
                -1,
                null,
                null,
                RMCGhostTargetTooltipJobKind.None));
        }
    }

    private IEnumerable<TargetData> GetPlayerTargets(EntityUid except)
    {
        var query = EntityQueryEnumerator<MetaDataComponent, MindContainerComponent>();
        while (query.MoveNext(out var uid, out var meta, out var mindContainer))
        {
            if (uid == except)
                continue;

            if (_ghostQuery.HasComp(uid))
                continue;

            if (HasComp<BrainComponent>(uid) ||
                HasComp<BorgBrainComponent>(uid) ||
                HasComp<MMIComponent>(uid))
            {
                continue;
            }

            if (!mindContainer.EverHadMind)
                continue;

            var displayName = meta.EntityName;
            var jobName = _jobs.MindTryGetJobName(mindContainer.Mind);
            var health = GetHealthStatus(uid);
            var tactical = GetTacticalIcons(uid);
            var tooltipKind = jobName == null
                ? RMCGhostTargetTooltipJobKind.None
                : HasComp<XenoComponent>(uid)
                    ? RMCGhostTargetTooltipJobKind.Caste
                    : RMCGhostTargetTooltipJobKind.Job;
            var searchText = jobName != null
                ? $"{displayName} {jobName}"
                : displayName;

            yield return new TargetData(uid, new RMCGhostTargetEntry(
                GetNetEntity(uid),
                displayName,
                searchText,
                jobName,
                false,
                GetFollowerCount(uid),
                health.Icon,
                health.Percent,
                tactical.Icon,
                tactical.Background,
                tooltipKind));
        }
    }

    private IEnumerable<TargetData> GetGhostTargets(EntityUid except, bool showAdminGhosts)
    {
        foreach (var player in _player.Sessions)
        {
            if (player.AttachedEntity is not { Valid: true } attached)
                continue;

            if (attached == except ||
                !TryComp<GhostComponent>(attached, out var ghost))
            {
                continue;
            }

            if (!showAdminGhosts && ghost.CanGhostInteract)
                continue;

            var displayName = Name(attached);
            TryComp<MindContainerComponent>(attached, out var mindContainer);
            var jobName = _jobs.MindTryGetJobName(mindContainer?.Mind);
            var searchText = jobName != null
                ? $"{displayName} {jobName}"
                : displayName;
            var tooltipKind = jobName != null
                ? RMCGhostTargetTooltipJobKind.Job
                : RMCGhostTargetTooltipJobKind.None;

            yield return new TargetData(attached, new RMCGhostTargetEntry(
                GetNetEntity(attached),
                displayName,
                searchText,
                jobName,
                false,
                GetFollowerCount(attached),
                null,
                -1,
                null,
                null,
                tooltipKind));
        }
    }

    private (SpriteSpecifier.Rsi? Icon, int Percent) GetHealthStatus(EntityUid uid)
    {
        if (!_mobState.IsCritical(uid) && !_mobState.IsAlive(uid))
            return (null, -1);

        if (!TryComp<DamageableComponent>(uid, out var damageable) ||
            !TryComp<MobThresholdsComponent>(uid, out var thresholds) ||
            !_mobThreshold.TryGetThresholdForState(uid, MobState.Dead, out var deadThreshold, thresholds))
        {
            return (null, -1);
        }

        var maxHealth = deadThreshold.Value.Float();
        if (maxHealth <= 0)
            return (null, -1);

        var currentHealth = maxHealth - damageable.TotalDamage.Float();
        var percent = Math.Clamp((int) MathF.Round(currentHealth / maxHealth * 100f), 0, 100);
        var state = percent >= 80
            ? "health_high"
            : percent >= 40
                ? "health_medium"
                : "health_low";

        return (new SpriteSpecifier.Rsi(new ResPath("/Textures/_RMC14/Interface/health_hud.rsi"), state), percent);
    }

    private (SpriteSpecifier.Rsi? Icon, SpriteSpecifier.Rsi? Background) GetTacticalIcons(EntityUid uid)
    {
        return TryComp<TacticalMapIconComponent>(uid, out var icon)
            ? (icon.Icon, icon.Background)
            : (null, null);
    }

    private int GetFollowerCount(EntityUid uid)
    {
        return TryComp<FollowedComponent>(uid, out var followed)
            ? followed.Following.Count
            : 0;
    }

    private void SortSection(SectionBuilder section)
    {
        section.Entries.Sort((a, b) => string.Compare(a.DisplayName, b.DisplayName, StringComparison.CurrentCulture));
        section.Children.Sort(CompareSectionsByTitle);

        foreach (var child in section.Children)
            SortSection(child);
    }

    private int CompareSectionsByTitle(SectionBuilder a, SectionBuilder b)
    {
        var aOthers = IsOthersSection(a);
        var bOthers = IsOthersSection(b);

        if (aOthers && !bOthers)
            return 1;
        if (bOthers && !aOthers)
            return -1;

        return string.Compare(GetSectionSortTitle(a), GetSectionSortTitle(b), StringComparison.CurrentCulture);
    }

    private string GetSectionSortTitle(SectionBuilder section)
    {
        if (!string.IsNullOrEmpty(section.Title))
            return section.Title;

        return string.IsNullOrEmpty(section.TitleLocId.Id)
            ? string.Empty
            : Loc.GetString(section.TitleLocId);
    }

    private static bool IsOthersSection(SectionBuilder section)
    {
        return section.Title == null && section.TitleLocId == OthersTitle;
    }

    private static bool HasContent(SectionBuilder section)
    {
        return section.Entries.Count > 0 || section.Children.Any(HasContent);
    }

    private static void AssignIndexes(List<SectionBuilder> sections)
    {
        for (var i = 0; i < sections.Count; i++)
        {
            sections[i].Index = i;
            AssignIndexes(sections[i].Children);
        }
    }

    private static RMCGhostTargetSection ToSection(SectionBuilder section)
    {
        var children = section.Children
            .Where(HasContent)
            .Select(ToSection)
            .ToList();

        return new RMCGhostTargetSection(
            section.Index,
            section.TitleLocId,
            section.Title,
            section.HeaderColor,
            section.IsExpandedByDefault,
            section.Entries,
            children);
    }

    private static Color AdjustLightness(Color color, float percent)
    {
        var hsv = Color.ToHsv(color);
        if (percent > 0)
            hsv.Z = Math.Min(hsv.Z * (1f + percent), 1f);
        else
            hsv.Z *= 1f + percent;

        return Color.FromHsv(hsv);
    }

    private void WarpTo(EntityUid uid, EntityUid target)
    {
        _adminLog.Add(LogType.GhostWarp, $"{ToPrettyString(uid)} RMC ghost warped to {ToPrettyString(target)}");

        if ((TryComp(target, out WarpPointComponent? warp) && warp.Follow) ||
            HasComp<MobStateComponent>(target) ||
            _ghostQuery.HasComp(target))
        {
            _followerSystem.StartFollowingEntity(uid, target);
            return;
        }

        var xform = Transform(uid);
        _transformSystem.SetCoordinates(uid, xform, Transform(target).Coordinates);
        _transformSystem.AttachToGridOrMap(uid, xform);
        if (_physicsQuery.TryComp(uid, out var physics))
            _physics.SetLinearVelocity(uid, Vector2.Zero, body: physics);
    }

    private readonly record struct TargetData(EntityUid Uid, RMCGhostTargetEntry Entry);

    private sealed class FactionSectionCollection(
        Dictionary<ProtoId<NpcFactionPrototype>, SectionBuilder> all,
        List<SectionBuilder> roots)
    {
        public readonly Dictionary<ProtoId<NpcFactionPrototype>, SectionBuilder> All = all;
        public readonly List<SectionBuilder> Roots = roots;
    }

    private sealed class SectionBuilder
    {
        public SectionBuilder(
            LocId titleLocId,
            string? title = null,
            Color? headerColor = null,
            bool isExpandedByDefault = true)
        {
            TitleLocId = titleLocId;
            Title = title;
            HeaderColor = headerColor ?? Color.FromHex("#696969");
            IsExpandedByDefault = isExpandedByDefault;
        }

        public int Index;
        public LocId TitleLocId;
        public string? Title;
        public Color HeaderColor;
        public bool IsExpandedByDefault;
        public readonly List<RMCGhostTargetEntry> Entries = new();
        public readonly List<SectionBuilder> Children = new();
    }
}
