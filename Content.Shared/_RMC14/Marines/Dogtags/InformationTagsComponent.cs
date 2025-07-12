using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Marines.Dogtags;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]

public sealed partial class InformationTagsComponent : Component
{
    [DataField, AutoNetworkedField]
    public List<InfoTagInfo> Tags = new();

    [DataField, AutoNetworkedField]
    public int MaxDisplayTags = 24;
}

[Serializable, NetSerializable]
public struct InfoTagInfo
{
    public string Name;
    public string Assignment;
    public string BloodType;
    //TODO RMC-14 Ghosts are also stored on the tags for the memorial
}

[Serializable, NetSerializable]
public enum InfoTagVisuals
{
    Number,
}
