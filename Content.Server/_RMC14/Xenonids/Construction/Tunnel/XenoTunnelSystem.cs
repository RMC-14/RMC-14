using Content.Shared._RMC14.Xenonids;
using Content.Shared._RMC14.Xenonids.Construction;
using Content.Shared._RMC14.Xenonids.Construction.ResinHole;
using Content.Shared._RMC14.Xenonids.Construction.Tunnel;
using Content.Shared._RMC14.Xenonids.Egg;
using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared._RMC14.Xenonids.Plasma;
using Content.Shared._RMC14.Xenonids.Weeds;
using Content.Shared.Actions;
using Content.Shared.Coordinates;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Movement.Events;
using Content.Shared.Popups;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Server._RMC14.Xenonids.Construction.Tunnel;

public abstract partial class XenoTunnelSystem : SharedXenoTunnelSystem
{
    //private const string TunnelPrototypeId = "XenoTunnel";

    [Dependency] private readonly SharedXenoHiveSystem _hive = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly EntityManager _entities = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly XenoPlasmaSystem _xenoPlasma = default!;
    [Dependency] private readonly SharedXenoWeedsSystem _xenoWeeds = default!;
    [Dependency] private readonly SharedXenoResinHoleSystem _xenoResinHole = default!;
    [Dependency] private readonly SharedXenoConstructionSystem _xenoConstruct = default!;
    private readonly RobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<XenoComponent, XenoDigTunnelActionEvent>(OnCreateTunnel);
        SubscribeLocalEvent<XenoComponent, XenoDigTunnelDoAfter>(OnFinishCreateTunnel);

        SubscribeLocalEvent<XenoTunnelComponent, InteractHandEvent>(OnInteract);
        SubscribeLocalEvent<XenoTunnelComponent, ContainerRelayMovementEntityEvent>(OnAttemptMoveInTunnel);
        SubscribeLocalEvent<XenoTunnelComponent, TraverseXenoTunnelMessage>(OnMoveThroughTunnel);

        SubscribeLocalEvent<XenoTunnelComponent, EnterXenoTunnelDoAfterEvent>(OnFinishEnterTunnel);
        SubscribeLocalEvent<XenoTunnelComponent, TraverseXenoTunnelDoAfterEvent>(OnFinishMoveThroughTunnel);

        SubscribeLocalEvent<XenoTunnelComponent, OpenBoundInterfaceMessage>(GetAllAvailableTunnels);
        SubscribeLocalEvent<XenoTunnelComponent, NameTunnelMessage>(NameTunnel);
    }
    private void OnCreateTunnel(Entity<XenoComponent> xenoBuilder, ref XenoDigTunnelActionEvent args)
    {
        if (args.Handled)
        {
            return;
        }

        var location = _transform.GetMoverCoordinates(xenoBuilder).SnapToGrid(_entities);
        if (!CanPlaceTunnel(args.Performer, location))
        {
            return;
        }

        if (_transform.GetGrid(location) is not { } gridId ||
            !TryComp(gridId, out MapGridComponent? grid))
        {
            return;
        }

        if (!_xenoPlasma.HasPlasmaPopup(xenoBuilder.Owner, args.PlasmaCost, false))
        {
            return;
        }

        if (_xenoWeeds.GetWeedsOnFloor((gridId, grid), location, true) is EntityUid weedSource)
        {
            XenoPlaceResinTunnelDestroyWeedSourceDoAfterEvent weedRemovalEv = new()
            {
                CreateTunnelDelay = args.CreateTunnelDelay,
                PlasmaCost = args.PlasmaCost,
                Prototype = args.Prototype
            };

            var doAfterWeedRemovalArgs = new DoAfterArgs(EntityManager, xenoBuilder.Owner, args.DestroyWeedSourceDelay, weedRemovalEv, xenoBuilder.Owner, weedSource)
            {
                BlockDuplicate = true,
                BreakOnMove = true,
                DuplicateCondition = DuplicateConditions.SameTarget
            };
            _doAfter.TryStartDoAfter(doAfterWeedRemovalArgs);
            _popup.PopupEntity(Loc.GetString("rmc-xeno-construction-resin-tunnel-uproot"), args.Performer, args.Performer);
            return;
        }
        _xenoPlasma.TryRemovePlasma(xenoBuilder.Owner, args.PlasmaCost);
        var createTunnelEv = new XenoDigTunnelDoAfter(args.Prototype, args.PlasmaCost);
        var doAfterTunnelCreationArgs = new DoAfterArgs(EntityManager, xenoBuilder.Owner, args.CreateTunnelDelay, createTunnelEv, xenoBuilder.Owner)
        {
            BlockDuplicate = true,
            BreakOnMove = true,
            DuplicateCondition = DuplicateConditions.SameTarget
        };
        _doAfter.TryStartDoAfter(doAfterTunnelCreationArgs);
        _popup.PopupEntity(Loc.GetString("rmc-xeno-construction-resin-tunnel-uproot"), args.Performer, args.Performer);
    }

    private void OnCompleteRemoveWeedSource(Entity<XenoComponent> xenoBuilder, ref XenoPlaceResinTunnelDestroyWeedSourceDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
        {
            return;
        }

        if (args.Target is null)
        {
            return;
        }

        var location = _transform.GetMoverCoordinates(xenoBuilder).SnapToGrid(EntityManager);

        if (!_xenoPlasma.HasPlasmaPopup(xenoBuilder.Owner, args.PlasmaCost, false))
        {
            return;
        }

        _xenoPlasma.TryRemovePlasma(xenoBuilder.Owner, args.PlasmaCost);
        var createTunnelEv = new XenoDigTunnelDoAfter(args.Prototype, args.PlasmaCost);
        var doAfterTunnelCreationArgs = new DoAfterArgs(EntityManager, xenoBuilder.Owner, args.CreateTunnelDelay, createTunnelEv, xenoBuilder.Owner)
        {
            BlockDuplicate = true,
            BreakOnMove = true,
            DuplicateCondition = DuplicateConditions.SameTarget
        };
        _doAfter.TryStartDoAfter(doAfterTunnelCreationArgs);
        _popup.PopupEntity(Loc.GetString("rmc-xeno-construction-resin-tunnel-uproot"), xenoBuilder.Owner, xenoBuilder.Owner);
    }
    private void OnFinishCreateTunnel(Entity<XenoComponent> xenoBuilder, ref XenoDigTunnelDoAfter args)
    {
        if (args.Cancelled || args.Handled)
        {
            return;
        }
        var location = _transform.GetMoverCoordinates(xenoBuilder).SnapToGrid(_entities);
        if (!CanPlaceTunnel(xenoBuilder.Owner, location))
        {
            return;
        }
        EntityUid? newTunnelEnt = null;
        while (!TryPlaceTunnel(xenoBuilder.Owner, Loc.GetString("rmc-xeno-construction-default-tunnel-name", ("tunnelNumber", _random.Next(0, 100000))), out newTunnelEnt))
        {

        }

        _ui.OpenUi(newTunnelEnt.Value, NameTunnelUI.Key, xenoBuilder.Owner);
    }
    private void OnInteract(Entity<XenoTunnelComponent> xenoTunnel, ref InteractHandEvent args)
    {
        if (args.Handled)
        {
            return;
        }

        var enteringEntity = args.User;

        if (!HasComp<XenoComponent>(enteringEntity))
        {
            var mobContainer = _container.EnsureContainer<BaseContainer>(xenoTunnel.Owner, XenoTunnelComponent.ContainedMobsContainerId);
            if (mobContainer.Count == 0)
                _popup.PopupClient(Loc.GetString("rmc-xeno-construction-tunnel-empty-non-xeno-enter-failure"), enteringEntity);
            else
                _popup.PopupClient(Loc.GetString("rmc-xeno-construction-tunnel-occupied-non-xeno-enter-failure"), enteringEntity);
            return;
        }

        var ev = new EnterXenoTunnelDoAfterEvent();
        var doAfterArgs = new DoAfterArgs(_entities, enteringEntity, xenoTunnel.Comp.EnterDelay, ev, xenoTunnel.Owner)
        {
            BreakOnMove = true,

        };
        _doAfter.TryStartDoAfter(doAfterArgs);
    }

    private void OnAttemptMoveInTunnel(Entity<XenoTunnelComponent> xenoTunnel, ref ContainerRelayMovementEntityEvent args)
    {
        _transform.PlaceNextTo(args.Entity, xenoTunnel.Owner);
    }

    private void OnMoveThroughTunnel(Entity<XenoTunnelComponent> xenoTunnel, ref TraverseXenoTunnelMessage args)
    {
        var destinationTunnel = _entities.GetEntity(args.DestinationTunnel);
        if (!HasComp<XenoTunnelComponent>(destinationTunnel))
        {
            return;
        }
        var ev = new TraverseXenoTunnelDoAfterEvent();
        var doAfterArgs = new DoAfterArgs(_entities, _entities.GetEntity(args.Entity), xenoTunnel.Comp.MoveDelay, ev, destinationTunnel);
    }

    private void OnFinishEnterTunnel(Entity<XenoTunnelComponent> xenoTunnel, ref EnterXenoTunnelDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
        {
            return;
        }
        var (ent, comp) = xenoTunnel;
        var enteringEntity = args.User;

        var mobContainer = _container.EnsureContainer<BaseContainer>(ent, XenoTunnelComponent.ContainedMobsContainerId);
        _container.Insert(enteringEntity, mobContainer);
        _ui.OpenUi(ent, SelectDestinationTunnelUI.Key, enteringEntity);
    }

    private void OnFinishMoveThroughTunnel(Entity<XenoTunnelComponent> destinationXenoTunnel, ref TraverseXenoTunnelDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
        {
            return;
        }
        var (ent, comp) = destinationXenoTunnel;
        var enteringEntity = args.User;

        var mobContainer = _container.EnsureContainer<BaseContainer>(ent, XenoTunnelComponent.ContainedMobsContainerId);

        _container.Insert(enteringEntity, mobContainer);
    }

    private void GetAllAvailableTunnels(Entity<XenoTunnelComponent> destinationXenoTunnel, ref OpenBoundInterfaceMessage args)
    {
        var hive = _hive.GetHive(destinationXenoTunnel.Owner);
        if (hive is null ||
            !TryComp(hive, out HiveComponent? hiveComp))
        {
            return;
        }


        var hiveTunnels = hiveComp.HiveTunnels;
        Dictionary<string, NetEntity> netHiveTunnels = new();
        foreach (var (name, tunnel) in hiveTunnels)
        {
            netHiveTunnels.Add(name, _entities.GetNetEntity(tunnel));
        }

        var newState = new SelectDestinationTunnelInterfaceState(netHiveTunnels);

        _ui.SetUiState(destinationXenoTunnel.Owner, SelectDestinationTunnelUI.Key, newState);
    }

    private void NameTunnel(Entity<XenoTunnelComponent> xenoTunnel, ref NameTunnelMessage args)
    {

    }

    private List<string> GetAllHiveTunnelNames(Entity<HiveComponent> hive)
    {
        List<string> tunnels = hive.Comp.HiveTunnels.Keys.ToList();
        return tunnels;
    }

    private bool CanPlaceTunnel(EntityUid user, EntityCoordinates coords)
    {
        var canPlaceStructure = _xenoConstruct.CanPlaceXenoStructure(user, coords, out var popupType, false);

        if (!canPlaceStructure)
        {
            popupType = popupType + "-resin-tunnel";
            _popup.PopupEntity(popupType, user, user, PopupType.SmallCaution);
            return false;
        }
        return true;
    }
    public bool TryPlaceTunnel(Entity<HiveMemberComponent?> builder, string name, [NotNullWhen(true)] out EntityUid? tunnelEnt)
    {
        tunnelEnt = null;
        if (!Resolve(builder, ref builder.Comp) ||
            builder.Comp.Hive is null)
        {
            return false;
        }

        return TryPlaceTunnel(builder.Comp.Hive.Value, name, builder.Owner.ToCoordinates(), out tunnelEnt);
    }
}
