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
using Robust.Shared.Serialization;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Shared._RMC14.Xenonids.Construction.Tunnel;

public abstract partial class SharedXenoTunnelSystem : EntitySystem
{
    private const string TunnelPrototypeId = "XenoTunnel";

    [Dependency] private readonly SharedXenoHiveSystem _hive = default!;


    public bool TryPlaceTunnel(EntityUid associatedHiveEnt, string name, EntityCoordinates buildLocation, [NotNullWhen(true)] out EntityUid? tunnelEnt)
    {
        tunnelEnt = null;
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
        tunnelEnt = newTunnel;

        _hive.SetHive(newTunnel, associatedHiveEnt);

        return hiveComp.HiveTunnels.TryAdd(name, newTunnel);
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
    public int PlasmaCost = 200;
    public string Prototype;
    public XenoDigTunnelDoAfter(EntProtoId prototype, int plasmaCost)
    {
        PlasmaCost = plasmaCost;
        Prototype = prototype;
    }
}

[Serializable, NetSerializable]
public sealed partial class XenoPlaceResinTunnelDestroyWeedSourceDoAfterEvent : SimpleDoAfterEvent
{
    [DataField]
    public EntProtoId Prototype = "XenoTunnel";

    [DataField]
    public float CreateTunnelDelay = 4.0f;

    [DataField]
    public int PlasmaCost = 200;
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
public sealed partial class NameTunnelMessage : BoundUserInterfaceMessage
{
    public string TunnelName;
    public NameTunnelMessage(string tunnelName)
    {
        TunnelName = tunnelName;
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


[Serializable, NetSerializable]
public sealed partial class XenoDigTunnelActionEvent : InstantActionEvent
{
    [DataField]
    public EntProtoId Prototype = "XenoTunnel";

    [DataField]
    public float DestroyWeedSourceDelay = 1.0f;

    [DataField]
    public float CreateTunnelDelay = 4.0f;

    [DataField]
    public int PlasmaCost = 200;
}

[Serializable, NetSerializable]
public enum SelectDestinationTunnelUI
{
    Key
}

[Serializable, NetSerializable]
public enum NameTunnelUI
{
    Key
}
