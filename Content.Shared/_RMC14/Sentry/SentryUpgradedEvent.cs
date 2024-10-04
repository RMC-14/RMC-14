namespace Content.Shared._RMC14.Sentry;

[ByRefEvent]
public readonly record struct SentryUpgradedEvent(EntityUid OldSentry, EntityUid NewSentry, EntityUid User);
