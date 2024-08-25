using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.Xenonids.Crippling;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(XenoCripplingStrikeSystem))]
public sealed partial class VictimCripplingStrikeSlowedComponent : Component
{
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan ExpireAt;

    [DataField, AutoNetworkedField]
    public FixedPoint2 SpeedMultiplier = FixedPoint2.New(0.75);

    [DataField, AutoNetworkedField]
    public float DamageMult = 1;

    [DataField, AutoNetworkedField]
    public bool WasHit = false;
}
