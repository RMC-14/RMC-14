using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Stamina;

[RegisterComponent, NetworkedComponent]
public sealed partial class RMCStaminaDamageOnHitComponent : Component
{
    [DataField]
    public double Damage;
}
