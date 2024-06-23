using Content.Shared.Chemistry.Components;
using Content.Shared.FixedPoint;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Medical.Scanner;

[Serializable, NetSerializable]
public sealed class HealthScannerBuiState(
    NetEntity target,
    FixedPoint2 blood,
    FixedPoint2 maxBlood,
    float? temperature,
    Solution? chemicals,
    bool bleeding)
    : BoundUserInterfaceState
{
    public readonly NetEntity Target = target;
    public readonly FixedPoint2 Blood = blood;
    public readonly FixedPoint2 MaxBlood = maxBlood;
    public readonly float? Temperature = temperature;
    public readonly Solution? Chemicals = chemicals;
    public readonly bool Bleeding = bleeding;
}

[Serializable, NetSerializable]
public enum HealthScannerUIKey
{
    Key
}
