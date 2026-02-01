using Content.Shared._RMC14.Map;
using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared._RMC14.Xenonids.Plasma;
using Content.Shared._RMC14.Xenonids.Construction.Events;
using Content.Shared.Actions;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.DoAfter;
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
    private const int PlasmaCost = 250;
    private static readonly TimeSpan Cooldown = TimeSpan.FromSeconds(30);
    private const float Range = 7f;
    private const float Timeup = 1f;

    private static readonly TimeSpan BuildTime = TimeSpan.FromSeconds(Timeup);
    private const string XenoStructuresAnimation = "RMCEffect";
    private const string AnimationChoice = "WallXenoResinThick";

    [Dependency] private readonly IMapManager _map = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly TurfSystem _turf = default!;
    [Dependency] private readonly SharedMapSystem _sharedMap = default!;
    [Dependency] private readonly RMCMapSystem _rmcMap = default!;
    [Dependency] private readonly SharedXenoHiveSystem _hive = default!;
    [Dependency] private readonly XenoPlasmaSystem _xenoPlasma = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    private const string WallPrototype = "WallXenoResinThickSurge";

    public override void Initialize()
    {
        SubscribeLocalEvent<DesignerStrainComponent, DesignerGreaterResinSurgeActionEvent>(OnAction);
        SubscribeLocalEvent<DesignerStrainComponent, Events.DesignerGreaterResinSurgeDoAfterEvent>(OnDoAfter);
    }

    private void OnAction(Entity<DesignerStrainComponent> ent, ref DesignerGreaterResinSurgeActionEvent args)
    {
        if (args.Handled)
            return;

        // Server-authoritative cooldown gate (action useDelay is UI-side).
        if (_net.IsServer && _timing.CurTime < ent.Comp.NextGreaterResinSurgeAt)
        {
            _popup.PopupClient(Loc.GetString("rmc-xeno-designer-greater-surge-cooldown"), ent, ent, PopupType.SmallCaution);
            return;
        }

        // Quick pre-check; plasma is charged on completion.
        if (!_xenoPlasma.HasPlasmaPopup(ent.Owner, PlasmaCost))
            return;

        // Clear any leftover effects from an interrupted do-after.
        if (_net.IsServer && ent.Comp.GreaterResinSurgeEffects.Count > 0)
        {
            foreach (var effect in ent.Comp.GreaterResinSurgeEffects)
                QueueDel(effect);

            ent.Comp.GreaterResinSurgeEffects.Clear();
            ent.Comp.GreaterResinSurgeDoAfter = null;
        }

        // Spawn the standard xeno construction animation effects immediately, so the 1s build time is visible.
        if (_net.IsServer)
        {
            var userCoords = Transform(ent.Owner).Coordinates;
            if (_transform.GetGrid(userCoords) is { } gridId &&
                TryComp<MapGridComponent>(gridId, out var grid) &&
                grid != null)
            {
                var effectId = XenoStructuresAnimation + AnimationChoice;
                if (_prototype.HasIndex(effectId))
                {
                    foreach (var turf in _sharedMap.GetTilesIntersecting(gridId, grid,
                                 Box2.CenteredAround(userCoords.Position, new(Range * 2, Range * 2)), false))
                    {
                        var tileCenter = _turf.GetTileCenter(turf);
                        if (!_transform.InRange(userCoords, tileCenter, Range))
                            continue;

                        var hasNode = false;
                        using (var anchoredNodes = _rmcMap.GetAnchoredEntitiesEnumerator<DesignNodeComponent>(tileCenter))
                        {
                            while (anchoredNodes.MoveNext(out var nodeUid))
                            {
                                if (!TryComp(nodeUid, out DesignNodeComponent? nodeComp))
                                    continue;

                                if (nodeComp.NodeType is not (DesignNodeType.Optimized or DesignNodeType.Flexible or DesignNodeType.Construct))
                                    continue;

                                hasNode = true;
                                break;
                            }
                        }

                        if (!hasNode)
                            continue;

                        var effect = Spawn(effectId, tileCenter);
                        ent.Comp.GreaterResinSurgeEffects.Add(effect);
                        RaiseNetworkEvent(
                            new XenoConstructionAnimationStartEvent(GetNetEntity(effect), GetNetEntity(ent.Owner), BuildTime),
                            Filter.PvsExcept(effect)
                        );
                    }
                }
            }
        }

        var ev = new Events.DesignerGreaterResinSurgeDoAfterEvent();
        var doAfter = new DoAfterArgs(EntityManager, ent.Owner, Timeup, ev, ent.Owner)
        {
            BreakOnMove = true,
            DuplicateCondition = DuplicateConditions.SameEvent,
        };

        // Start the do-after; walls are spawned on completion.
        var started = _doAfter.TryStartDoAfter(doAfter, out var doAfterId);
        args.Handled = started;

        if (_net.IsServer)
        {
            if (started)
            {
                ent.Comp.GreaterResinSurgeDoAfter = doAfterId;
            }
            else
            {
                // If we failed to start the do-after, remove any spawned animation effects.
                foreach (var effect in ent.Comp.GreaterResinSurgeEffects)
                    QueueDel(effect);
                ent.Comp.GreaterResinSurgeEffects.Clear();
                ent.Comp.GreaterResinSurgeDoAfter = null;
            }
        }
    }

    private void OnDoAfter(Entity<DesignerStrainComponent> ent, ref Events.DesignerGreaterResinSurgeDoAfterEvent args)
    {
        if (_net.IsServer && ent.Comp.GreaterResinSurgeEffects.Count > 0)
        {
            foreach (var effect in ent.Comp.GreaterResinSurgeEffects)
                QueueDel(effect);
            ent.Comp.GreaterResinSurgeEffects.Clear();
            ent.Comp.GreaterResinSurgeDoAfter = null;
        }

        if (args.Cancelled)
            return;

        // Check grid
        var userCoords = Transform(ent.Owner).Coordinates;
        if (_transform.GetGrid(userCoords) is not { } gridId ||
            !TryComp<MapGridComponent>(gridId, out var grid) ||
            grid == null)
            return;

        // Server-authoritative cooldown and plasma
        if (_net.IsServer)
        {
            if (_timing.CurTime < ent.Comp.NextGreaterResinSurgeAt)
            {
                _popup.PopupClient(Loc.GetString("rmc-xeno-designer-greater-surge-cooldown"), ent, ent, PopupType.SmallCaution);
                return;
            }

            if (!_xenoPlasma.TryRemovePlasmaPopup(ent.Owner, PlasmaCost))
                return;

            ent.Comp.NextGreaterResinSurgeAt = _timing.CurTime + Cooldown;

            var affected = 0;
            foreach (var turf in _sharedMap.GetTilesIntersecting(gridId, grid,
                         Box2.CenteredAround(userCoords.Position, new(Range * 2, Range * 2)), false))
            {
                var tileCenter = _turf.GetTileCenter(turf);
                if (!_transform.InRange(userCoords, tileCenter, Range))
                    continue;

                var nodesToConvert = new List<EntityUid>();
                using (var anchoredNodes = _rmcMap.GetAnchoredEntitiesEnumerator<DesignNodeComponent>(tileCenter))
                {
                    while (anchoredNodes.MoveNext(out var nodeUid))
                    {
                        if (!TryComp(nodeUid, out DesignNodeComponent? nodeComp))
                            continue;

                        if (nodeComp.NodeType is not (DesignNodeType.Optimized or DesignNodeType.Flexible or DesignNodeType.Construct))
                            continue;

                        nodesToConvert.Add(nodeUid);
                    }
                }

                if (nodesToConvert.Count == 0)
                    continue;

                // Spawn after the enumerator is disposed to avoid modifying anchored entity collections mid-iteration.
                var spawned = Spawn(WallPrototype, tileCenter.SnapToGrid(EntityManager, _map));
                _hive.SetSameHive(ent.Owner, spawned);

                foreach (var nodeUid in nodesToConvert)
                {
                    QueueDel(nodeUid);
                    affected++;
                }
            }

            if (affected == 0)
                _popup.PopupClient(Loc.GetString("rmc-xeno-designer-greater-surge-none"), ent, ent, PopupType.SmallCaution);
            else
                _popup.PopupClient(Loc.GetString("rmc-xeno-designer-greater-surge-success", ("count", affected)), ent, ent, PopupType.Small);
        }
    }
}
