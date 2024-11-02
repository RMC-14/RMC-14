using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Marines.Dogtags;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TakeableTagsComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool TagsTaken = false;

    [DataField]
    public EntProtoId InfoTag = "RMCInformationDogtag";
}

[Serializable, NetSerializable]
public enum DogtagVisuals
{
    Taken,
}
