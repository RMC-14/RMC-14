using Content.Shared.Alert;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._CM14.NightVision;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class NightVisionComponent : Component
{
    [DataField]
    public AlertType Alert = AlertType.XenoNightVision;

    [DataField, AutoNetworkedField]
    public NightVisionState State = NightVisionState.Half;
}

[Serializable, NetSerializable]
public enum NightVisionState
{
    Off,
    Half,
    Full
}
