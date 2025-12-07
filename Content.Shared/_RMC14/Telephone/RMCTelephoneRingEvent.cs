namespace Content.Shared._RMC14.Telephone;

/// <summary>
///     Raised when playing rotary telephone ring sound.
/// </summary>
[ByRefEvent]
public readonly record struct RMCTelephoneRingEvent(EntityUid Receiving, EntityUid Calling, EntityUid Actor);
