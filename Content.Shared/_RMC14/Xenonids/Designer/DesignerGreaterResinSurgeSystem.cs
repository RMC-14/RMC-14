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

    public override void Initialize()
    {
        SubscribeLocalEvent<DesignerStrainComponent, DesignerGreaterResinSurgeActionEvent>(OnAction);
        SubscribeLocalEvent<DesignerStrainComponent, EntityTerminatingEvent>(OnDesignerTerminating);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_net.IsServer)
            return;

        var now = _timing.CurTime;
        var pendingQuery = EntityQueryEnumerator<DesignerGreaterResinSurgePendingComponent>();
        while (pendingQuery.MoveNext(out var uid, out var pending))
        {
            if (!Exists(pending.Designer))
            {
                CleanupEffects(pending);
                QueueDel(uid);
                continue;
            }

            if (now < pending.EndTime)
                continue;

            CleanupEffects(pending);
            CompleteSurge(pending.Designer, pending);
            QueueDel(uid);
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
        var pendingQuery = EntityQueryEnumerator<DesignerGreaterResinSurgePendingComponent>();
        while (pendingQuery.MoveNext(out _, out var pending))
        {
            if (pending.Designer == ent.Owner)
                return;
        }

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
        var tileCenters = new List<NetCoordinates>();
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
                tileCenters.Add(GetNetCoordinates(tileCenter));
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
                var effect = Spawn(effectId, GetCoordinates(tileCenter));
                effects.Add(effect);
                if (TryGetNetEntity(effect, out var netEffect) && TryGetNetEntity(ent.Owner, out var netUser))
                {
                    RaiseNetworkEvent(
                        new XenoConstructionAnimationStartEvent(netEffect.Value, netUser.Value, ent.Comp.GreaterResinSurgeBuildTime),
                        Filter.PvsExcept(effect)
                    );
                }
            }
        }

        var pendingUid = Spawn(null, MapCoordinates.Nullspace);
        var pendingComp = new DesignerGreaterResinSurgePendingComponent
        {
            Designer = ent.Owner,
            EndTime = _timing.CurTime + ent.Comp.GreaterResinSurgeBuildTime,
            Origin = GetNetCoordinates(userCoords),
            Grid = gridId,
            Range = range,
            AnimationEffect = ent.Comp.GreaterResinSurgeAnimationEffect,
            WallPrototype = ent.Comp.GreaterResinSurgeWallPrototype,
            BuildTime = ent.Comp.GreaterResinSurgeBuildTime,
            TileCenters = tileCenters,
            Effects = effects,
        };

        AddComp(pendingUid, pendingComp, true);

        args.Handled = true;
    }

    private void CompleteSurge(EntityUid user, DesignerGreaterResinSurgePendingComponent pending)
    {
        if (!TryComp(user, out DesignerStrainComponent? designer))
            return;

        var affected = 0;
        foreach (var netTileCenter in pending.TileCenters)
        {
            var tileCenter = GetCoordinates(netTileCenter);
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
            var spawned = Spawn(pending.WallPrototype, tileCenter.SnapToGrid(EntityManager, _map));
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

    private void CleanupEffects(DesignerGreaterResinSurgePendingComponent pending)
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

        var pendingQuery = EntityQueryEnumerator<DesignerGreaterResinSurgePendingComponent>();
        while (pendingQuery.MoveNext(out var uid, out var pending))
        {
            if (pending.Designer != ent.Owner)
                continue;

            CleanupEffects(pending);
            QueueDel(uid);
        }
    }
}
