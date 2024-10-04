using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Xenonids.Parasite;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ParasiteOverlayComponent : Component
{
    [DataField, AutoNetworkedField]
    public int NumPositions = 4;

    //Should equal num positions
    [DataField, AutoNetworkedField]
    public bool[] VisiblePostitions = [false, false, false, false];
}

[Serializable, NetSerializable]
public enum ParasiteOverlayVisuals
{
    Resting,
    Downed,
    State
}

[Serializable, NetSerializable]
public enum ParasiteOverlayLayers
{
    RightArm,
    Head,
    LeftArm,
    Back
}

public sealed class ParasiteOverlayFlags { }
