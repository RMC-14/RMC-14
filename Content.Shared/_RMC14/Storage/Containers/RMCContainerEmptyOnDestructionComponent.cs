using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Storage.Containers;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCContainerEmptyOnDestructionComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool OnDelete = true;

    [DataField, AutoNetworkedField]
    public bool OnDestruction = true;
}
