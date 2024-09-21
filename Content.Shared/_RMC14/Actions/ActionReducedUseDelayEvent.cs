using Content.Shared.FixedPoint;

namespace Content.Shared._RMC14.Actions;

// If amount is 0, will reset usedelay to default value
// If amount is between 0 and 1, will reduce usedelay
public record struct ActionReducedUseDelayEvent(FixedPoint2 Amount);
