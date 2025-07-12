using Content.Shared.Damage;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Fax.Components;

/// <summary>
/// RMC-specific fax component which stores a damage specifier for attempting to fax a mob.
/// This is RMC-specific functionality that extends the base fax system.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCFaxecuteComponent : Component
{
    /// <summary>
    /// Type of damage dealt when entity is faxecuted.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public DamageSpecifier Damage = new();
}
