using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Wieldable.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RMCWieldableSystem))]
public sealed partial class WieldableSpeedModifiersComponent : Component
{
    [DataField, AutoNetworkedField]
    public float BaseWalk = 1f;

    [DataField, AutoNetworkedField]
    public float ModifiedWalk = 1f;

    [DataField, AutoNetworkedField]
    public float BaseSprint = 1f;

    [DataField, AutoNetworkedField]
    public float ModifiedSprint = 1f;
}
