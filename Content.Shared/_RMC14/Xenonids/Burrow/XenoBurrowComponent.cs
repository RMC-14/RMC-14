using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Shared._RMC14.Xenonids.Burrow;
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class XenoBurrowComponent : Component
{
    /// <summary>
    /// Whether the xeno is currently burrowed or not
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Active = false;

    /// <summary>
    /// Max distance a xeno can move from burrowing
    /// </summary>
    [DataField]
    public float MaxTunnelingDistance = 15;

    /// <summary>
    /// How long it takes for the xeno to burrow down
    /// </summary>
    [DataField]
    public TimeSpan BurrowLength = TimeSpan.FromSeconds(1.5);

    /// <summary>
    /// How long the xeno must wait before burrowing back up or tunneling
    /// </summary>
    [DataField]
    public TimeSpan TunnelCooldown = TimeSpan.FromSeconds(2);

    public TimeSpan? NextTunnelAt;

    /// <summary>
    /// How long the xeno can stay burrowed
    /// </summary>
    [DataField]
    public TimeSpan BurrowMaxDuration = TimeSpan.FromSeconds(9);

    public TimeSpan? ForcedUnburrowAt;

    /// <summary>
    /// Distance from unburrow coordinates which entities will be stunned
    /// </summary>
    [DataField]
    public float UnburrowStunRange = 0.5f;

    /// <summary>
    /// How long the entities in unburrow stun range will be stunned for
    /// </summary>
    [DataField]
    public TimeSpan UnburrowStunLength = TimeSpan.FromSeconds(4);

    [DataField]
    public SoundSpecifier BurrowDownSound = new SoundPathSpecifier("/Audio/_RMC14/Xeno/burrowing_b.ogg");

    [DataField]
    public SoundSpecifier BurrowUpSound = new SoundPathSpecifier("/Audio/_RMC14/Xeno/burrowoff.ogg");
}
