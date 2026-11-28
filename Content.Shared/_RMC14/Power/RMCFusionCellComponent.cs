using Robust.Shared.GameStates;

using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Power;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedRMCPowerSystem))]
public sealed partial class RMCFusionCellComponent : Component
{
    [DataField, AutoNetworkedField]
    public float Fuel = 100;

    [DataField, AutoNetworkedField]
    public float MaxFuel = 100;

    [DataField, AutoNetworkedField]
    public bool IsFresh = true;

    public float FuelPercentage => MaxFuel <= 0 ? 0 : Fuel / MaxFuel;
}

[Serializable, NetSerializable]
public enum RMCFusionCellVisuals
{
    Fuel,
}

[Serializable, NetSerializable]
public enum RMCFusionCellFuelLevel
{
    Empty,
    Low,
    Medium,
    High,
    Full,
}
