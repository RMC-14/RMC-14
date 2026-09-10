namespace Content.Shared._RMC14.Cryostorage;

/// <summary>
/// Raised after a stored body leaves vanilla cryostorage, usually through reconnection.
/// Recovery consoles listen for it so they stop showing equipment that is no longer stored.
/// </summary>
[ByRefEvent]
public readonly record struct LeftCryostorageEvent;
