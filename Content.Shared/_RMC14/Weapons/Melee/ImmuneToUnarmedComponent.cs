using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Weapons.Melee;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedRMCMeleeWeaponSystem))]
public sealed partial class ImmuneToUnarmedComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool ApplyToXenos;
}
