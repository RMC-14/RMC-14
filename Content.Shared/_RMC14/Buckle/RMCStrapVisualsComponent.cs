using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Buckle;

[RegisterComponent, NetworkedComponent]
public sealed partial class RMCStrapVisualsComponent : Component;

[Serializable, NetSerializable]
public enum StrapVisuals : byte
{
    StrapState,
}

[Serializable, NetSerializable]
public enum StrapState : byte
{
    Strapped,
    Unstrapped,
}
