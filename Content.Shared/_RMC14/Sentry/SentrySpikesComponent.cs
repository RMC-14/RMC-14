using Content.Shared.Damage;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Sentry;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SentrySystem))]
public sealed partial class SentrySpikesComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public DamageSpecifier SpikeDamage = new();

    [DataField(required: true), AutoNetworkedField]
    public string AnimationState;

    [DataField(required: true), AutoNetworkedField]
    public TimeSpan AnimationTime;

	[DataField, AutoNetworkedField]
	public string Layer = "sentry";
}
