using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Sensor;

[Serializable, NetSerializable]
public sealed partial class SensorTowerRepairDoAfterEvent : SimpleDoAfterEvent
{
    [DataField]
    public SensorTowerState State;

    public SensorTowerRepairDoAfterEvent(SensorTowerState state)
    {
        State = state;
    }
}
