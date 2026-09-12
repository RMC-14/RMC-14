using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.LastStand;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class XenoLastStandComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool CallToArmsDone = false;
}
