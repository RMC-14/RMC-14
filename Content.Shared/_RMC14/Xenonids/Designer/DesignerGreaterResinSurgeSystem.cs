using Content.Shared._RMC14.Map;
using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared._RMC14.Xenonids.Plasma;
using Content.Shared._RMC14.Xenonids.Construction.Events;
using Content.Shared.Actions;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.Popups;
using Content.Shared.Maps;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

// TODO: Make greater resin surge crate reflective resin instead of thick resin.
namespace Content.Shared._RMC14.Xenonids.Designer;

public sealed class DesignerGreaterResinSurgeSystem : EntitySystem
{
    private const float EffectSearchPaddingMultiplier = 2f;

    private readonly record struct PendingSurge(
        TimeSpan EndTime,
        EntityCoordinates Origin,
        EntityUid Grid,
        float Range,
        EntProtoId AnimationEffect,
        EntProtoId WallPrototype,
        TimeSpan BuildTime,
        List<EntityCoordinates> TileCenters,
        List<EntityUid> Effects
    );

    [Dependency] private readonly IMapManager _map = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly TurfSystem _turf = default!;
    [Dependency] private readonly SharedMapSystem _sharedMap = default!;
    [Dependency] private readonly RMCMapSystem _rmcMap = default!;
    [Dependency] private readonly SharedXenoHiveSystem _hive = default!;
    [Dependency] private readonly XenoPlasmaSystem _xenoPlasma = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    private readonly Dictionary<EntityUid, PendingSurge> _pending = new();

    public override void Initialize()
    {
        SubscribeLocalEvent<DesignerStrainComponent, DesignerGreaterResinSurgeActionEvent>(OnAction);
        SubscribeLocalEvent<DesignerStrainComponent, EntityTerminatingEvent>(OnDesignerTerminating);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_net.IsServer || _pending.Count == 0)
            return;

        var now = _timing.CurTime;
        List<EntityUid>? completed = null;
        List<EntityUid>? removed = null;

        foreach (var (uid, pending) in _pending)
        {
            if (!Exists(uid))
            {
                removed ??= new List<EntityUid>(4);
                removed.Add(uid);
                continue;
            }

            if (now < pending.EndTime)
                continue;

            completed ??= new List<EntityUid>(4);
            completed.Add(uid);
        }

        if (removed != null)
        {
            foreach (var uid in removed)
            {
                if (_pending.Remove(uid, out var pending))
                    CleanupEffects(pending);
            }
        }

        if (completed != null)
        {
            foreach (var uid in completed)
            {
                if (!_pending.Remove(uid, out var pending))
                    continue;

                CleanupEffects(pending);
                CompleteSurge(uid, pending);
            }
        }
    }

    private void OnAction(Entity<DesignerStrainComponent> ent, ref DesignerGreaterResinSurgeActionEvent args)
    {
        if (args.Handled)
            return;

        // Greater Resin Surge is server-authoritative.
        if (!_net.IsServer)
        {
            args.Handled = true;
            return;
        }

        // One surge wind-up at a time.
        if (_pending.ContainsKey(ent.Owner))
            return;

        // Server-authoritative cooldown gate (action useDelay is UI-side).
        if (_timing.CurTime < ent.Comp.NextGreaterResinSurgeAt)
        {
            _popup.PopupClient(Loc.GetString("rmc-xeno-designer-greater-surge-cooldown"), ent, ent, PopupType.SmallCaution);
            return;
        }

        // Quick pre-check; plasma is charged immediately on successful activation.
        if (!_xenoPlasma.HasPlasmaPopup(ent.Owner, ent.Comp.GreaterResinSurgePlasmaCost))
            return;

        if (!TryComp(ent.Owner, out TransformComponent? entXform))
            return;

        var userCoords = entXform.Coordinates;
        if (_transform.GetGrid(userCoords) is not { } gridId ||
            !TryComp<MapGridComponent>(gridId, out var grid) ||
            grid == null)
            return;

        // Determine what tiles are eligible at activation time (so user movement doesn't matter).
        var range = ent.Comp.GreaterResinSurgeRange;
        var tileCenters = new List<EntityCoordinates>();
        foreach (var turf in _sharedMap.GetTilesIntersecting(gridId, grid,
                     Box2.CenteredAround(userCoords.Position, new(range * EffectSearchPaddingMultiplier, range * EffectSearchPaddingMultiplier)), false))
        {
            var tileCenter = _turf.GetTileCenter(turf);
            if (!_transform.InRange(userCoords, tileCenter, range))
                continue;

            var hasEligibleNode = false;
            using (var anchoredNodes = _rmcMap.GetAnchoredEntitiesEnumerator<DesignNodeComponent>(tileCenter))
            {
                while (anchoredNodes.MoveNext(out var nodeUid))
                {
                    if (!TryComp(nodeUid, out DesignNodeComponent? nodeComp))
                        continue;

                    if (nodeComp.NodeType is not (DesignNodeType.Optimized or DesignNodeType.Flexible or DesignNodeType.Construct))
                        continue;

                    // Only surge your own nodes.
                    if (nodeComp.BoundXeno != ent.Owner)
                        continue;

                    hasEligibleNode = true;
                    break;
                }
            }

            if (hasEligibleNode)
                tileCenters.Add(tileCenter);
        }

        if (tileCenters.Count == 0)
        {
            _popup.PopupClient(Loc.GetString("rmc-xeno-designer-greater-surge-none"), ent, ent, PopupType.SmallCaution);
            return;
        }

        if (!_xenoPlasma.TryRemovePlasmaPopup(ent.Owner, ent.Comp.GreaterResinSurgePlasmaCost))
            return;

        ent.Comp.NextGreaterResinSurgeAt = _timing.CurTime + ent.Comp.GreaterResinSurgeCooldown;

        // Spawn build animation effects for the wind-up.
        var effects = new List<EntityUid>(tileCenters.Count);
        var effectId = ent.Comp.GreaterResinSurgeAnimationEffect;
        if (_prototype.HasIndex(effectId))
        {
            foreach (var tileCenter in tileCenters)
            {
                var effect = Spawn(effectId, tileCenter);
                effects.Add(effect);
                RaiseNetworkEvent(
                    new XenoConstructionAnimationStartEvent(GetNetEntity(effect), GetNetEntity(ent.Owner), ent.Comp.GreaterResinSurgeBuildTime),
                    Filter.PvsExcept(effect)
                );
            }
        }

        _pending[ent.Owner] = new PendingSurge(
            _timing.CurTime + ent.Comp.GreaterResinSurgeBuildTime,
            userCoords,
            gridId,
            range,
            ent.Comp.GreaterResinSurgeAnimationEffect,
            ent.Comp.GreaterResinSurgeWallPrototype,
            ent.Comp.GreaterResinSurgeBuildTime,
            tileCenters,
            effects
        );

        args.Handled = true;
    }

    private void CompleteSurge(EntityUid user, PendingSurge pending)
    {
        if (!TryComp(user, out DesignerStrainComponent? designer))
            return;

        var affected = 0;
        foreach (var tileCenter in pending.TileCenters)
        {
            var nodesToConvert = new List<EntityUid>();
            using (var anchoredNodes = _rmcMap.GetAnchoredEntitiesEnumerator<DesignNodeComponent>(tileCenter))
            {
                while (anchoredNodes.MoveNext(out var nodeUid))
                {
                    if (!TryComp(nodeUid, out DesignNodeComponent? nodeComp))
                        continue;

                    if (nodeComp.NodeType is not (DesignNodeType.Optimized or DesignNodeType.Flexible or DesignNodeType.Construct))
                        continue;

                    if (nodeComp.BoundXeno != user)
                        continue;

                    nodesToConvert.Add(nodeUid);
                }
            }

            if (nodesToConvert.Count == 0)
                continue;

            // Spawn after the enumerator is disposed to avoid modifying anchored entity collections mid-iteration.
            var spawned = Spawn(designer.GreaterResinSurgeWallPrototype, tileCenter.SnapToGrid(EntityManager, _map));
            _hive.SetSameHive(user, spawned);

            foreach (var nodeUid in nodesToConvert)
            {
                QueueDel(nodeUid);
                affected++;
            }
        }

        if (affected == 0)
            _popup.PopupClient(Loc.GetString("rmc-xeno-designer-greater-surge-none"), user, user, PopupType.SmallCaution);
        else
            _popup.PopupClient(Loc.GetString("rmc-xeno-designer-greater-surge-success", ("count", affected)), user, user, PopupType.Small);
    }

    private void CleanupEffects(PendingSurge pending)
    {
        foreach (var effect in pending.Effects)
        {
            if (Exists(effect))
                QueueDel(effect);
        }
    }

    private void OnDesignerTerminating(Entity<DesignerStrainComponent> ent, ref EntityTerminatingEvent args)
    {
        if (!_net.IsServer)
            return;

        if (_pending.Remove(ent.Owner, out var pending))
            CleanupEffects(pending);
    }
}
