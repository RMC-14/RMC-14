using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._CM14.Marines.HyperSleep;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(SharedHyperSleepChamberSystem))]
public sealed partial class HyperSleepChamberComponent : Component
{
    [DataField, AutoNetworkedField]
    public string ContainerId = "cm-hypersleep";
}

[Serializable, NetSerializable]
public enum HyperSleepChamberLayers
{
    Base
}
