using Content.Shared._RMC14.Marines.Skills;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Weapons.Ranged.Recoil;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RMCGunRecoilSystem))]
public sealed partial class RMCGunRecoilComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntProtoId<SkillDefinitionComponent> Skill = "RMCSkillFirearms";

    [DataField, AutoNetworkedField]
    public bool HasRecoilBuildup = false;

    [DataField, AutoNetworkedField]
    public int Strength = 0;

    [DataField, AutoNetworkedField]
    public int StrengthUnwielded = 0;

    /// <summary>
    /// Amount recoil is added if the user is unskilled
    /// </summary>
    [DataField, AutoNetworkedField]
    public int UnskilledStrength = 1;

    /// <summary>
    /// Amount recoil is removed if the user is unskilled
    /// </summary>
    [DataField, AutoNetworkedField]
    public int SkilledStrength = 1;

    /// <summary>
    /// Recoil buildup loss per second
    /// </summary>
    [DataField, AutoNetworkedField]
    public float RecoilLossPerSecond = 5;

    [DataField, AutoNetworkedField]
    public float RecoilBuildup = 0;

    [DataField, AutoNetworkedField]
    public float MaximumRecoilBuildup = 0.5f;
}
