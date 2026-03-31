using Content.Shared.Chemistry.Components;
using Content.Shared.FixedPoint;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Medical.Scanner;

/// <summary>
///     Pure scan state — no BUI dependency. Used by body scanner snapshots, stored medical
///     records, and as the payload inside <see cref="HealthScannerBuiState"/>.
/// </summary>
[DataRecord, Serializable, NetSerializable]
public readonly record struct HealthScanState(
    NetEntity Target,
    FixedPoint2 Blood,
    FixedPoint2 MaxBlood,
    float? Temperature,
    string Pulse,
    Solution? Chemicals,
    bool Bleeding,
    HealthScanDetailLevel DetailLevel);

/// <summary>
///     Thin BUI wrapper around <see cref="HealthScanState"/> for the health analyzer live-update path.
/// </summary>
[Serializable, NetSerializable]
public sealed class HealthScannerBuiState(HealthScanState scanState) : BoundUserInterfaceState
{
    public readonly HealthScanState ScanState = scanState;
}

[Serializable, NetSerializable]
public enum HealthScannerUIKey
{
    Key
}

[Serializable, NetSerializable]
public enum HealthScanDetailLevel : byte
{
    HealthAnalyzer = 0,
    BodyScan = 1,
    Full = 2,
}
