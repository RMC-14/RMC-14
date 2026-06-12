namespace Content.Shared._RMC14.Cryostorage;

/// <summary>
/// Raised after vanilla cryostorage has moved a player into stored state.
/// RMC sidecar systems use this to refresh recovery UIs without replacing the upstream storage flow.
/// </summary>
[ByRefEvent]
public readonly record struct EnteredCryostorageEvent;
