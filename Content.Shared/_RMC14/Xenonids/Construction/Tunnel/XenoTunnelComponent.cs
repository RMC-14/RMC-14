using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Shared._RMC14.Xenonids.Construction.Tunnel;

/// <summary>
/// All xenos of the same hive of the tunnel may select a different tunnel, that is also from their hive,
/// to go to, everyone else are sent to a random tunnel 
/// </summary>
[RegisterComponent]
public sealed partial class XenoTunnelComponent : Component
{
    public const string ContainedMobsContainerId = "rmc_xeno_tunnel_mob_container";

    /// <summary>
    /// How long it takes to enter this tunnel
    /// </summary>
    [DataField]
    public TimeSpan EnterDelay = new(0, 0, 6);

    /// <summary>
    /// How long it takes to move from this tunnel to another one
    /// </summary>
    [DataField]
    public TimeSpan MoveDelay = new(0, 0, 4);
}
