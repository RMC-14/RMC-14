using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.Xenonids.Heal;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(XenoHealSystem))]
public sealed partial class XenoBeingHealedComponent : Component
{
    [DataField, AutoNetworkedField]
    public FixedPoint2 Amount;

    [DataField, AutoNetworkedField]
    public TimeSpan Duration;

    [DataField, AutoNetworkedField]
    public TimeSpan TimeBetweenHeals;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan NextHealAt;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan Start;
}
