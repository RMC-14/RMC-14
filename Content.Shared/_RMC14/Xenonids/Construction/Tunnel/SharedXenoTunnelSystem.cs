using Content.Shared._RMC14.Xenonids;
using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared.Actions;
using Content.Shared.Coordinates;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Movement.Events;
using Content.Shared.Popups;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Shared._RMC14.Xenonids.Construction.Tunnel;

public abstract partial class SharedXenoTunnelSystem : EntitySystem
{
    private const string TunnelPrototypeId = "XenoTunnel";

    [Dependency] private readonly SharedXenoHiveSystem _hive = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly EntityManager _entities = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedTransformSystem  _transform = default!;
    [Dependency] private readonly SharedUserInterfaceSystem  _ui = default!;
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
    }
    private void OnCreateTunnel(Entity<XenoComponent> xenoBuilder, ref XenoDigTunnelActionEvent args)
    {

    }

    private void OnFinishCreateTunnel(Entity<XenoComponent> xenoBuilder, ref XenoDigTunnelDoAfter args)
    {
        if (args.Cancelled || args.Handled)
        {
            return;
        }

        
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
        if (!HasComp< XenoTunnelComponent>(destinationTunnel))
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
    private List<string> GetAllHiveTunnelNames(Entity<HiveComponent> hive)
    {
        List<string> tunnels = hive.Comp.HiveTunnels.Keys.ToList();
        return tunnels;
    }
    public bool TryPlaceTunnel(Entity<HiveMemberComponent?> builder, string name)
    {
        if (!Resolve(builder, ref builder.Comp) ||
            builder.Comp.Hive is null)
        {
            return false;
        }

        return TryPlaceTunnel(builder.Comp.Hive.Value, name, builder.Owner.ToCoordinates());
    }

    public bool TryPlaceTunnel(EntityUid associatedHiveEnt, string name, EntityCoordinates buildLocation)
    {
        if (!TryComp(associatedHiveEnt, out HiveComponent? hiveComp))
        {
            return false;
        }
        var tunnels = hiveComp.HiveTunnels;
        if (tunnels.ContainsKey(name))
        {
            return false;
        }

        var newTunnel = Spawn(TunnelPrototypeId, buildLocation);
        _hive.SetHive(newTunnel, associatedHiveEnt);

        hiveComp.HiveTunnels.Add(name, newTunnel);
        return true;
    }
}

/// <summary>
/// Do after event raised on the tunnel when an entity is entering the tunnel
/// </summary>
[Serializable, NetSerializable]
public sealed partial class EnterXenoTunnelDoAfterEvent : SimpleDoAfterEvent
{

}

/// <summary>
/// Do after event raised on the destination tunnel when an entity is moving between 2 tunnels
/// </summary>
[Serializable, NetSerializable]
public sealed partial class TraverseXenoTunnelDoAfterEvent : SimpleDoAfterEvent
{

}

/// <summary>
/// Do after event raised on the xeno that finished building a tunnel
/// </summary>
[Serializable, NetSerializable]
public sealed partial class XenoDigTunnelDoAfter : SimpleDoAfterEvent
{

}

[Serializable, NetSerializable]
public sealed partial class TraverseXenoTunnelMessage : BoundUserInterfaceMessage
{
    public NetEntity DestinationTunnel;
    public TraverseXenoTunnelMessage(NetEntity destinationTunnel)
    {
        DestinationTunnel = destinationTunnel;
    }
}

[Serializable, NetSerializable]
public sealed partial class SelectDestinationTunnelInterfaceState : BoundUserInterfaceState
{
    public Dictionary<string, NetEntity> HiveTunnels;
    public SelectDestinationTunnelInterfaceState(Dictionary<string, NetEntity> hiveTunnels)
    {
        HiveTunnels = hiveTunnels;
    }
}
public sealed partial class XenoDigTunnelActionEvent : InstantActionEvent
{
    [DataField]
    public EntProtoId Prototype = "XenoTunnel";

    [DataField]
    public float DestroyWeedSourceDelay = 1.0f;

    [DataField]
    public int PlasmaCost = 200;
}

public enum SelectDestinationTunnelUI
{
    Key
}
