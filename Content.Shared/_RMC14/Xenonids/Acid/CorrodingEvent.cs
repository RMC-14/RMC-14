namespace Content.Shared._RMC14.Xenonids.Acid;

/// <summary>
/// Raised on an entity when a xeno corrodes it with acid.
/// If this event is not cancelled, it will add <see cref="TimedCorrodingComponent"/>.
/// Cancel this if you want to have special corrosion logic, e.g. <see cref="DamageableCorrodingComponent"/>.
/// </summary>
[ByRefEvent]
public record struct CorrodingEvent(EntityUid Acid, float Dps, float ExpendableLightDps, bool Cancelled = false);

[ByRefEvent]
public record struct BeforeMeltedEvent();
