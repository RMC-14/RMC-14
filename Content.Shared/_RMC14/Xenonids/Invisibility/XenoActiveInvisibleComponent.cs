using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.Xenonids.Invisibility;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(XenoInvisibilitySystem))]
public sealed partial class XenoActiveInvisibleComponent : Component
{
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan ExpiresAt;

    [DataField, AutoNetworkedField]
    public TimeSpan FullCooldown = TimeSpan.FromSeconds(20);

    [DataField, AutoNetworkedField]
    public FixedPoint2 SpeedMultiplier = FixedPoint2.New(1.15);

    [DataField, AutoNetworkedField]
    public bool DidPopup;
}
