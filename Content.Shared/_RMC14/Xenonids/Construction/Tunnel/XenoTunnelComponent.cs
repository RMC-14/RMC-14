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

    [DataField]
    public int MaxMobs = 3;

    /// How long it takes to enter this tunnel

    [DataField]
    public TimeSpan SmallXenoEnterDelay = new(0, 0, 1);

    [DataField]
    public TimeSpan StandardXenoEnterDelay = new(0, 0, 4);

    [DataField]
    public TimeSpan LargeXenoEnterDelay = new(0, 0, 12);

    /// How long it takes to move from this tunnel to another one

    [DataField]
    public TimeSpan SmallXenoMoveDelay = new(0, 0, 1);

    [DataField]
    public TimeSpan StandardXenoMoveDelay = new(0, 0, 2);

    [DataField]
    public TimeSpan LargeXenoMoveDelay = new(0, 0, 6);
}
