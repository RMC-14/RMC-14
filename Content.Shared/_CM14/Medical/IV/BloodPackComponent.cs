using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._CM14.Medical.IV;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class BloodPackComponent : Component
{
    [DataField, AutoNetworkedField]
    public string Solution = "pack";
}

[Serializable, NetSerializable]
public enum BloodPackVisuals
{
    Label
}
