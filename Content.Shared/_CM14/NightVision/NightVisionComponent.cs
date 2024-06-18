using Content.Shared.Alert;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._CM14.NightVision;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class NightVisionComponent : Component
{
    [DataField]
    public ProtoId<AlertPrototype> Alert = "XenoNightVision";

    [DataField, AutoNetworkedField]
    public NightVisionState State = NightVisionState.Full;
}

[Serializable, NetSerializable]
public enum NightVisionState
{
    Off,
    Half,
    Full
}
