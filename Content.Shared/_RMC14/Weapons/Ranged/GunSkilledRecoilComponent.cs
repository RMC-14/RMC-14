using Content.Shared._RMC14.Marines.Skills;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Weapons.Ranged;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SkillsSystem))]
public sealed partial class GunSkilledRecoilComponent : Component
{
    [DataField, AutoNetworkedField]
    public float SetRecoil;

    [DataField(required: true), AutoNetworkedField]
    public Dictionary<EntProtoId<SkillDefinitionComponent>, int> Skills = new();

    [DataField, AutoNetworkedField]
    public bool MustBeWielded = true;
}
