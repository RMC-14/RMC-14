using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Explosion;

/// <summary>
///     Stores the direction in which a breaching charge was planted.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RMCBreachingChargeSystem))]
public sealed partial class RMCBreachingChargeComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool DirectionSet;
}
