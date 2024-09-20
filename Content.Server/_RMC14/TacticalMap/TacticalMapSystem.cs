using System.Linq;
using Content.Server._RMC14.Announce;
using Content.Server._RMC14.Marines;
using Content.Shared._RMC14.CCVar;
using Content.Shared._RMC14.Marines;
using Content.Shared._RMC14.Marines.Skills;
using Content.Shared._RMC14.Marines.Squads;
using Content.Shared._RMC14.Rules;
using Content.Shared._RMC14.TacticalMap;
using Content.Shared._RMC14.Xenonids;
using Content.Shared.Actions;
using Content.Shared.Atmos.Rotting;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Roles;
using Content.Shared.Roles.Jobs;
using Content.Shared.UserInterface;
using Robust.Shared.Configuration;
using Robust.Shared.Map.Components;
using Robust.Shared.Timing;

namespace Content.Server._RMC14.TacticalMap;

public sealed class TacticalMapSystem : SharedTacticalMapSystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly SharedJobSystem _job = default!;
    [Dependency] private readonly MarineAnnounceSystem _marineAnnounce = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly SkillsSystem _skills = default!;
    [Dependency] private readonly SquadSystem _squad = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly XenoAnnounceSystem _xenoAnnounce = default!;

    private EntityQuery<ActiveTacticalMapTrackedComponent> _activeTacticalMapTrackedQuery;
    private EntityQuery<MarineComponent> _marineQuery;
    private EntityQuery<MapGridComponent> _mapGridQuery;
    private EntityQuery<MobStateComponent> _mobStateQuery;
    private EntityQuery<RottingComponent> _rottingQuery;
    private EntityQuery<SquadTeamComponent> _squadTeamQuery;
    private EntityQuery<TacticalMapComponent> _tacticalMapQuery;
    private EntityQuery<TransformComponent> _transformQuery;
    private EntityQuery<XenoComponent> _xenoQuery;

    private readonly HashSet<Entity<ActiveTacticalMapTrackedComponent>> _toUpdate = new();

    private TimeSpan _announceCooldown;

    public override void Initialize()
    {
        base.Initialize();

        _activeTacticalMapTrackedQuery = GetEntityQuery<ActiveTacticalMapTrackedComponent>();
        _marineQuery = GetEntityQuery<MarineComponent>();
        _mapGridQuery = GetEntityQuery<MapGridComponent>();
        _mobStateQuery = GetEntityQuery<MobStateComponent>();
        _rottingQuery = GetEntityQuery<RottingComponent>();
        _squadTeamQuery = GetEntityQuery<SquadTeamComponent>();
        _tacticalMapQuery = GetEntityQuery<TacticalMapComponent>();
        _transformQuery = GetEntityQuery<TransformComponent>();
        _xenoQuery = GetEntityQuery<XenoComponent>();

        SubscribeLocalEvent<TacticalMapComponent, MapInitEvent>(OnTacticalMapMapInit);

        SubscribeLocalEvent<TacticalMapUserComponent, MapInitEvent>(OnTacticalMapUserMapInit);
        SubscribeLocalEvent<TacticalMapUserComponent, OpenTacticalMapActionEvent>(OnTacticalMapUserOpenAction);

        SubscribeLocalEvent<TacticalMapComputerComponent, MapInitEvent>(OnComputerMapInit);
        SubscribeLocalEvent<TacticalMapComputerComponent, BeforeActivatableUIOpenEvent>(OnComputerBeforeUIOpen);

        SubscribeLocalEvent<TacticalMapTrackedComponent, MapInitEvent>(OnTrackedMapInit);
        SubscribeLocalEvent<TacticalMapTrackedComponent, MobStateChangedEvent>(OnTrackedMobStateChanged);

        SubscribeLocalEvent<ActiveTacticalMapTrackedComponent, ComponentRemove>(OnActiveRemove);
        SubscribeLocalEvent<ActiveTacticalMapTrackedComponent, EntityTerminatingEvent>(OnActiveRemove);
        SubscribeLocalEvent<ActiveTacticalMapTrackedComponent, MoveEvent>(OnActiveTrackedMove);
        SubscribeLocalEvent<ActiveTacticalMapTrackedComponent, RoleAddedEvent>(OnActiveTrackedRoleAdded);
        SubscribeLocalEvent<ActiveTacticalMapTrackedComponent, MindAddedMessage>(OnActiveTrackedMindAdded);
        SubscribeLocalEvent<ActiveTacticalMapTrackedComponent, SquadMemberUpdatedEvent>(OnActiveSquadMemberUpdated);

        SubscribeLocalEvent<RottingComponent, MapInitEvent>(OnRottingMapInit);
        SubscribeLocalEvent<RottingComponent, ComponentRemove>(OnRottingRemove);

        Subs.BuiEvents<TacticalMapUserComponent>(TacticalMapUserUi.Key,
            subs =>
            {
                subs.Event<TacticalMapUpdateCanvasMsg>(OnTacticalMapUserUpdateCanvasMsg);
            });

        Subs.BuiEvents<TacticalMapComputerComponent>(TacticalMapComputerUi.Key,
            subs =>
            {
                subs.Event<TacticalMapUpdateCanvasMsg>(OnTacticalMapComputerUpdateCanvasMsg);
            });

        Subs.CVar(_config,
            RMCCVars.RMCTacticalMapAnnounceCooldownSeconds,
            v => _announceCooldown = TimeSpan.FromSeconds(v),
            true);
    }

    private void OnTacticalMapMapInit(Entity<TacticalMapComponent> ent, ref MapInitEvent args)
    {
        var users = EntityQueryEnumerator<TacticalMapUserComponent>();
        while (users.MoveNext(out var userId, out var userComp))
        {
            userComp.Map = ent;
            Dirty(userId, userComp);
        }

        var computers = EntityQueryEnumerator<TacticalMapComputerComponent>();
        while (computers.MoveNext(out var computerId, out var computerComp))
        {
            computerComp.Map = ent;
            Dirty(computerId, computerComp);
        }
    }

    private void OnTacticalMapUserMapInit(Entity<TacticalMapUserComponent> ent, ref MapInitEvent args)
    {
        _actions.AddAction(ent, ref ent.Comp.Action, ent.Comp.ActionId);

        if (TryGetTacticalMap(out var map))
            ent.Comp.Map = map;

        Dirty(ent);
    }

    private void OnTacticalMapUserOpenAction(Entity<TacticalMapUserComponent> ent, ref OpenTacticalMapActionEvent args)
    {
        if (TryGetTacticalMap(out var map))
            UpdateUserData(ent, map);

        _ui.TryOpenUi(ent.Owner, TacticalMapUserUi.Key, ent);
    }

    private void OnComputerMapInit(Entity<TacticalMapComputerComponent> ent, ref MapInitEvent args)
    {
        if (TryGetTacticalMap(out var map))
            ent.Comp.Map = map;

        Dirty(ent);
    }

    private void OnComputerBeforeUIOpen(Entity<TacticalMapComputerComponent> ent, ref BeforeActivatableUIOpenEvent args)
    {
        if (!TryGetTacticalMap(out var map))
            return;

        UpdateMapData(ent, map);
    }

    private void OnTrackedMapInit(Entity<TacticalMapTrackedComponent> ent, ref MapInitEvent args)
    {
        var state = _mobStateQuery.CompOrNull(ent)?.CurrentState ?? MobState.Alive;
        UpdateActiveTracking(ent, state);
    }

    private void OnTrackedMobStateChanged(Entity<TacticalMapTrackedComponent> ent, ref MobStateChangedEvent args)
    {
        if (_timing.ApplyingState)
            return;

        UpdateActiveTracking(ent, args.NewMobState);
    }

    private void OnActiveRemove<T>(Entity<ActiveTacticalMapTrackedComponent> ent, ref T args)
    {
        BreakTracking(ent);
    }

    private void OnActiveTrackedMove(Entity<ActiveTacticalMapTrackedComponent> ent, ref MoveEvent args)
    {
        _toUpdate.Add(ent);
    }

    private void OnActiveTrackedRoleAdded(Entity<ActiveTacticalMapTrackedComponent> ent, ref RoleAddedEvent args)
    {
        UpdateIcon(ent);
        UpdateTracked(ent);
    }

    private void OnActiveTrackedMindAdded(Entity<ActiveTacticalMapTrackedComponent> ent, ref MindAddedMessage args)
    {
        UpdateIcon(ent);
        UpdateTracked(ent);
    }

    private void OnActiveSquadMemberUpdated(Entity<ActiveTacticalMapTrackedComponent> ent, ref SquadMemberUpdatedEvent args)
    {
        if (_squadTeamQuery.TryComp(args.Squad, out var squad))
            ent.Comp.Color = squad.Color;
    }

    private void OnRottingMapInit(Entity<RottingComponent> ent, ref MapInitEvent args)
    {
        if (_activeTacticalMapTrackedQuery.TryComp(ent, out var active))
            UpdateTracked((ent, active));
    }

    private void OnRottingRemove(Entity<RottingComponent> ent, ref ComponentRemove args)
    {
        if (_activeTacticalMapTrackedQuery.TryComp(ent, out var active))
            UpdateTracked((ent, active));
    }

    private void OnTacticalMapUserUpdateCanvasMsg(Entity<TacticalMapUserComponent> ent, ref TacticalMapUpdateCanvasMsg args)
    {
        var user = args.Actor;
        if (!ent.Comp.CanDraw)
            return;

        var lines = args.Lines;
        if (lines.Count > LineLimit)
            lines = lines[..LineLimit];

        var time = _timing.CurTime;
        if (time < ent.Comp.NextAnnounceAt)
            return;

        var nextAnnounce = time + _announceCooldown;
        ent.Comp.NextAnnounceAt = nextAnnounce;
        Dirty(ent);

        if (ent.Comp.Marines)
            UpdateCanvas(lines, true, false, user);

        if (ent.Comp.Xenos)
            UpdateCanvas(lines, false, true, user);
    }

    private void OnTacticalMapComputerUpdateCanvasMsg(Entity<TacticalMapComputerComponent> ent, ref TacticalMapUpdateCanvasMsg args)
    {
        var user = args.Actor;
        if (!_skills.HasSkill(user, ent.Comp.Skill, ent.Comp.SkillLevel))
            return;

        var lines = args.Lines;
        if (lines.Count > LineLimit)
            lines = lines[..LineLimit];

        var time = _timing.CurTime;
        if (time < ent.Comp.NextAnnounceAt)
            return;

        var nextAnnounce = time + _announceCooldown;
        ent.Comp.NextAnnounceAt = nextAnnounce;
        Dirty(ent);

        var computers = EntityQueryEnumerator<TacticalMapComputerComponent>();
        while (computers.MoveNext(out var uid, out var computer))
        {
            computer.NextAnnounceAt = nextAnnounce;
            Dirty(uid, computer);
        }

        UpdateCanvas(lines, true, false, user);
    }

    private void UpdateActiveTracking(Entity<TacticalMapTrackedComponent> tracked, MobState mobState)
    {
        if (!tracked.Comp.TrackDead && mobState == MobState.Dead)
        {
            RemCompDeferred<ActiveTacticalMapTrackedComponent>(tracked);
            return;
        }

        if (EnsureComp<ActiveTacticalMapTrackedComponent>(tracked, out var active))
            return;

        var activeEnt = new Entity<ActiveTacticalMapTrackedComponent>(tracked, active);
        UpdateIcon(activeEnt);
        UpdateRotting(activeEnt);
        UpdateColor(activeEnt);
    }

    private void BreakTracking(Entity<ActiveTacticalMapTrackedComponent> tracked)
    {
        if (!_tacticalMapQuery.TryComp(tracked.Comp.Map, out var tacticalMap))
            return;

        tacticalMap.MarineBlips.Remove(tracked.Owner.Id);
        tacticalMap.XenoBlips.Remove(tracked.Owner.Id);
        tacticalMap.MapDirty = true;
        tracked.Comp.Map = null;
    }

    private void UpdateIcon(Entity<ActiveTacticalMapTrackedComponent> tracked)
    {
        if (!_mind.TryGetMind(tracked, out var mindId, out _) ||
            !_job.MindTryGetJob(mindId, out _, out var jobProto) ||
            jobProto.MinimapIcon == null)
        {
            return;
        }

        tracked.Comp.Icon = jobProto.MinimapIcon;
    }

    private void UpdateRotting(Entity<ActiveTacticalMapTrackedComponent> tracked)
    {
        tracked.Comp.Undefibbable = _rottingQuery.HasComp(tracked);
    }

    private void UpdateColor(Entity<ActiveTacticalMapTrackedComponent> tracked)
    {
        if (_squad.TryGetMemberSquad(tracked.Owner, out var squad))
            tracked.Comp.Color = squad.Comp.Color;
        else if (_xenoQuery.HasComp(tracked))
            tracked.Comp.Color = Color.FromHex("#3A064D");
    }

    private void UpdateTracked(Entity<ActiveTacticalMapTrackedComponent> ent)
    {
        if (ent.Comp.Icon is not { } icon ||
            !_transformQuery.TryComp(ent.Owner, out var xform) ||
            xform.GridUid is not { } gridId ||
            !_mapGridQuery.TryComp(gridId, out var gridComp) ||
            !_tacticalMapQuery.TryComp(gridId, out var tacticalMap) ||
            !_transform.TryGetGridTilePosition((ent.Owner, xform), out var indices, gridComp))
        {
            BreakTracking(ent);
            return;
        }

        if (ent.Comp.Map != xform.GridUid)
        {
            BreakTracking(ent);
            ent.Comp.Map = xform.GridUid;
        }

        var blip = new TacticalMapBlip(indices, icon, ent.Comp.Color, ent.Comp.Undefibbable);
        if (_marineQuery.HasComp(ent))
        {
            tacticalMap.MarineBlips[ent.Owner.Id] = blip;
            tacticalMap.MapDirty = true;
        }

        if (_xenoQuery.HasComp(ent))
        {
            tacticalMap.XenoBlips[ent.Owner.Id] = blip;
            tacticalMap.MapDirty = true;
        }
    }

    private bool TryGetTacticalMap(out Entity<TacticalMapComponent> map)
    {
        var query = EntityQueryEnumerator<TacticalMapComponent>();
        while (query.MoveNext(out var uid, out var mapComp))
        {
            map = (uid, mapComp);
            return true;
        }

        map = default;
        return false;
    }

    private void UpdateMapData(Entity<TacticalMapComputerComponent> computer, TacticalMapComponent map)
    {
        computer.Comp.Blips = map.MarineBlips;
        Dirty(computer);

        var lines = EnsureComp<TacticalMapLinesComponent>(computer);
        lines.MarineLines = map.MarineLines;
        Dirty(computer, lines);
    }

    private void UpdateUserData(Entity<TacticalMapUserComponent> user, TacticalMapComponent map)
    {
        var lines = EnsureComp<TacticalMapLinesComponent>(user);
        if (user.Comp.Marines)
        {
            user.Comp.MarineBlips = user.Comp.LiveUpdate ? map.MarineBlips : map.LastUpdateMarineBlips;
            lines.MarineLines = map.MarineLines;
            lines.XenoLines.Clear();
        }

        if (user.Comp.Xenos)
        {
            user.Comp.XenoBlips = user.Comp.LiveUpdate ? map.XenoBlips : map.LastUpdateXenoBlips;
            lines.XenoLines = map.XenoLines;
            lines.MarineLines.Clear();
        }

        Dirty(user);
        Dirty(user, lines);
    }

    private void UpdateCanvas(List<TacticalMapLine> lines, bool marine, bool xeno, EntityUid user)
    {
        var maps = EntityQueryEnumerator<TacticalMapComponent>();
        while (maps.MoveNext(out var map))
        {
            map.MapDirty = true;

            if (marine)
            {
                map.MarineLines = lines;
                map.LastUpdateMarineBlips = map.MarineBlips.ToDictionary();
                _marineAnnounce.AnnounceToMarines("The UNMC tactical map has been updated.");
            }

            if (xeno)
            {
                map.XenoLines = lines;
                map.LastUpdateXenoBlips = map.XenoBlips.ToDictionary();
                _xenoAnnounce.AnnounceSameHive(user, "The Xenonid tactical map has been updated.");
            }
        }
    }

    public override void Update(float frameTime)
    {
        try
        {
            foreach (var update in _toUpdate)
            {
                if (!update.Comp.Running)
                    continue;

                UpdateTracked(update);
            }
        }
        finally
        {
            _toUpdate.Clear();
        }

        var time = _timing.CurTime;
        var maps = EntityQueryEnumerator<TacticalMapComponent, RMCPlanetComponent>();
        while (maps.MoveNext(out var map, out _))
        {
            if (!map.MapDirty || time < map.NextUpdate)
                continue;

            map.MapDirty = false;
            map.NextUpdate = time + map.UpdateEvery;

            var computers = EntityQueryEnumerator<TacticalMapComputerComponent>();
            while (computers.MoveNext(out var computerId, out var computer))
            {
                if (!_ui.IsUiOpen(computerId, TacticalMapComputerUi.Key))
                    continue;

                UpdateMapData((computerId, computer), map);
            }

            var users = EntityQueryEnumerator<TacticalMapUserComponent>();
            while (users.MoveNext(out var userId, out var userComp))
            {
                if (!_ui.IsUiOpen(userId, TacticalMapUserUi.Key))
                    continue;

                UpdateUserData((userId, userComp), map);
            }
        }
    }
}
