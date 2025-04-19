using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Weapons.Melee;

/// <summary>
/// For melee weapons to apply extra damage to certain entities with certain component(s).
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedRMCMeleeWeaponSystem))]
public sealed partial class MeleeDamageMultiplierComponent : Component
{
    /// <summary>
    /// The amount the bonus damage is multipled by. For example, 0.5 would be x1.5 the weapon's base damage.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Multiplier = 0.5f; // x1.5 extra damage

    [DataField, AutoNetworkedField]
    public EntityWhitelist Whitelist = new();
}
