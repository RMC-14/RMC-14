using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Evacuation;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedEvacuationSystem))]
public sealed partial class EvacuationDoorComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Locked = true;
}
