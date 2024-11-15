using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Sensor;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SensorTowerSystem))]
public sealed partial class SensorTowerReceiverComponent : Component;
