using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Wieldable;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RMCWieldableSystem))]
public sealed partial class WieldSlowdownCompensationUserComponent : Component
{
    [DataField, AutoNetworkedField]
    public float Walk = 0f;

    [DataField, AutoNetworkedField]
    public float Sprint = 0f;
}
