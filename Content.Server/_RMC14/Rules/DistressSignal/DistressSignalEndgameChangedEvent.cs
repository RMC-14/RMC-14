namespace Content.Server._RMC14.Rules.DistressSignal;

/// <summary>
/// Raised when the distress signal rule starts or stops treating entities away
/// from the Almayer as escaped.
/// </summary>
public readonly record struct DistressSignalEndgameChangedEvent(bool Active);
