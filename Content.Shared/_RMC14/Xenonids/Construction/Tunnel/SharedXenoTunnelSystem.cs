using System.Diagnostics.CodeAnalysis;
using Content.Shared._RMC14.Areas;
using Content.Shared._RMC14.Xenonids.Egg;
using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Xenonids.Construction.Tunnel;

public abstract partial class SharedXenoTunnelSystem : EntitySystem
{
    private const string TunnelPrototypeId = "XenoTunnel";

    [Dependency] protected readonly SharedXenoHiveSystem Hive = default!;
    [Dependency] protected readonly AreaSystem Area = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private readonly List<string> _greekLetters = new()
        {
            "alpha",
            "beta",
            "gamma",
            "delta",
            "zeta",
            "theta",
            "phi",
            "psi",
            "omega"
        };
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<XenoTunnelComponent, ExaminedEvent>(OnExamine);
    }

    private void OnExamine(Entity<XenoTunnelComponent> xenoTunnel, ref ExaminedEvent args)
    {
        if (!HasComp<XenoComponent>(args.Examiner))
            return;

        if (!TryGetHiveTunnelName(xenoTunnel, out var tunnelName))
        {
            return;
        }

        using (args.PushGroup(nameof(XenoEggRetrieverComponent)))
        {
            args.PushMarkup(Loc.GetString("rmc-xeno-construction-tunnel-examine", ("tunnelName", tunnelName)));
        }
    }

    public bool TryGetHiveTunnelName(Entity<XenoTunnelComponent> xenoTunnel, [NotNullWhen(true)] out string? tunnelName)
    {
        tunnelName = null;
        var hive = Hive.GetHive(xenoTunnel.Owner);
        if (hive is null)
        {
            return false;
        }

        var hiveTunnels = hive.Value.Comp.HiveTunnels;

        foreach (var tunnel in hiveTunnels)
        {
            if (tunnel.Value == xenoTunnel.Owner)
            {
                tunnelName = tunnel.Key;
                return true;
            }
        }

        return false;
    }

    public bool TryPlaceTunnel(EntityUid associatedHiveEnt, string? name, EntityCoordinates buildLocation, [NotNullWhen(true)] out EntityUid? tunnelEnt)
    {
        tunnelEnt = null;
        if (!TryComp(associatedHiveEnt, out HiveComponent? hiveComp))
        {
            return false;
        }
        var tunnels = hiveComp.HiveTunnels;

        if (name is null)
        {
            var mapCoords = _transform.ToMapCoordinates(buildLocation.AlignWithClosestGridTile());
            var areaName = Loc.GetString("rmc-xeno-construction-default-area-name");
            var randomGreekLetter = _random.Pick(_greekLetters);
            if (Area.TryGetArea(buildLocation, out _, out var areaProto))
                areaName = areaProto.Name;

            name = Loc.GetString("rmc-xeno-construction-default-tunnel-name", ("areaName", areaName), ("coordX", mapCoords.X), ("coordY", mapCoords.Y), ("greekLetter", randomGreekLetter));
        }

        if (tunnels.ContainsKey(name))
        {
            return false;
        }

        var newTunnel = Spawn(TunnelPrototypeId, buildLocation);
        tunnelEnt = newTunnel;

        Hive.SetHive(newTunnel, associatedHiveEnt);

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
public sealed partial class XenoCollapseTunnelDoAfterEvent : SimpleDoAfterEvent
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
