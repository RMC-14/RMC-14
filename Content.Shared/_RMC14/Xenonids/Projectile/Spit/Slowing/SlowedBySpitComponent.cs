using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.Xenonids.Projectile.Spit.Slowing;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(XenoSpitSystem))]
public sealed partial class SlowedBySpitComponent : Component
{
	[DataField, AutoNetworkedField]
	public bool SuperSlow = true;

	[DataField, AutoNetworkedField]
    public float SuperMultiplier = 0.33f;

	[DataField, AutoNetworkedField]
	public float Multiplier = 0.66f;

	[DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan ExpiresAt;
}
