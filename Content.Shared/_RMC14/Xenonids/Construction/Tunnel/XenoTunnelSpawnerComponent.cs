using Robust.Shared.Prototypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Shared._RMC14.Xenonids.Construction.Tunnel;

/// <summary>
/// For mapping purposes, the gamerule should use the information in this component to spawn tunnels with the proper name and hive
/// </summary>
[RegisterComponent]
public sealed partial class XenoTunnelSpawnerComponent : Component
{
    [DataField]
    public string? TunnelName;

    [DataField]
    public string? HiveName;
}
