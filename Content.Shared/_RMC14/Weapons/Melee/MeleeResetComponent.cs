using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Weapons.Melee;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedRMCMeleeWeaponSystem))]
public sealed partial class MeleeResetComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan OriginalTime;
}
