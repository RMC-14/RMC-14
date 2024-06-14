using Content.Shared._CM14.Marines.Skills;
using Robust.Shared.GameStates;

namespace Content.Shared._CM14.Weapons.Ranged;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SkillsSystem))]
public sealed partial class GunSkilledRecoilComponent : Component
{
    [DataField, AutoNetworkedField]
    public float SetRecoil;

    [DataField(required: true), AutoNetworkedField]
    public Skills Skills;

    [DataField, AutoNetworkedField]
    public bool MustBeWielded = true;
}
