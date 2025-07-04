using Robust.Shared.Analyzers;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Stamina;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCStaminaDamageOnHitComponent : Component
{
    [DataField]
    public double Damage;

    [DataField, AutoNetworkedField]
    public bool RequiresWield = false;
}
