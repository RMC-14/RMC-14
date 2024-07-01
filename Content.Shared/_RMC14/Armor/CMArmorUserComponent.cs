using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Armor;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(CMArmorSystem))]
public sealed partial class CMArmorUserComponent : Component
{
    [DataField, AutoNetworkedField]
    public float WieldSlowdownCompensationWalk = 0f;

    [DataField, AutoNetworkedField]
    public float WieldSlowdownCompensationSprint = 0f;
}
