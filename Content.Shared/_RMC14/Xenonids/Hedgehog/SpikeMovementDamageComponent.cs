using Content.Shared.Damage;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.Hedgehog;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SpikeMovementDamageComponent : Component
{
    [DataField, AutoNetworkedField]
    public DamageSpecifier Damage = new();
    
    [DataField, AutoNetworkedField]
    public TimeSpan ExpiresAt;
}