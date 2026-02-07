using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Dropship;

[RegisterComponent, NetworkedComponent]
public sealed partial class LandingLightComponent : Component;

[Serializable, NetSerializable]
public enum LandingLightVisuals : byte
{
    State,
}

[Serializable, NetSerializable]
public enum LandingLightState : byte
{
    Off,
    On,
}
