namespace Content.Server._RMC14.ERT;

/// <summary>
/// Raised when ERT request state changes and admin/webhook views need refreshing.
/// </summary>
[ByRefEvent]
public readonly record struct RMCERTStateChangedEvent;
