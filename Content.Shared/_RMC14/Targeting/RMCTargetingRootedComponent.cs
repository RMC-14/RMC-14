using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Targeting;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedRMCTargetingSystem))]
public sealed partial class RMCTargetingRootedComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? Equipment;
}
