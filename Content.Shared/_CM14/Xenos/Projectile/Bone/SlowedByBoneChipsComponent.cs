using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._CM14.Xenos.Projectile.Bone;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(XenoBoneChipsSystem))]
public sealed partial class SlowedByBoneChipsComponent : Component
{
    [DataField, AutoNetworkedField]
    public float Multiplier = 0.5f;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan ExpiresAt;
}
