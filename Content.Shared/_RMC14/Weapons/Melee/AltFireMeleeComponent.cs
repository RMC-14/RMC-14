using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Weapons.Melee;

/// <summary>
/// This is used to allow ranged weapons to make melee attacks by right-clicking.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(SharedCMMeleeWeaponSystem))]
public sealed partial class AltFireMeleeComponent : Component
{
    [DataField, AutoNetworkedField]
    public AltFireAttackType AttackType = AltFireAttackType.Light;
}


[Flags]
public enum AltFireAttackType : byte
{
    Light = 0, // Standard single-target attack.
    Heavy = 1 << 0, // Wide swing.
    Disarm = 1 << 1
}
