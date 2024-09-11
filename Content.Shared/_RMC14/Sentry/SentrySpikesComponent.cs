using Content.Shared.Damage;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Sentry;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SentrySystem))]
public sealed partial class SentrySpikesComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public DamageSpecifier SpikeDamage = default!;
}
