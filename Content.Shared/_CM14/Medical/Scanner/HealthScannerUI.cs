using Content.Shared.Chemistry.Components;
using Robust.Shared.Serialization;

namespace Content.Shared._CM14.Medical.Scanner;

[Serializable, NetSerializable]
public sealed class HealthScannerBuiState : BoundUserInterfaceState
{
    public NetEntity Target;
    public float BloodPercentage;
    public float? Temperature;
    public Solution? Chemicals;

    public HealthScannerBuiState(NetEntity target, float bloodPercentage, float? temperature, Solution? chemicals)
    {
        Target = target;
        BloodPercentage = bloodPercentage;
        Temperature = temperature;
        Chemicals = chemicals;
    }
}

[Serializable, NetSerializable]
public enum HealthScannerUIKey
{
    Key
}
