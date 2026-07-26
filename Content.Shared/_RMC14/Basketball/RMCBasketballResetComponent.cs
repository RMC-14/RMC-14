using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Basketball;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true), AutoGenerateComponentPause]
[Access(typeof(RMCBasketballSystem))]
public sealed partial class RMCBasketballResetComponent : Component
{
    [DataField]
    public string CourtId = "basketball";

    [DataField]
    public TimeSpan ResetCooldown = TimeSpan.FromSeconds(5);

    [DataField]
    public TimeSpan PressedDuration = TimeSpan.FromSeconds(0.3);

    [DataField, AutoNetworkedField]
    public bool Pressed;

    [AutoPausedField]
    public TimeSpan NextResetAt;
}

[Serializable, NetSerializable]
public enum RMCBasketballResetLayers
{
    Base,
}
