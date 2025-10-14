namespace Content.Shared._RMC14.PowerLoader;

[ByRefEvent]
public record struct PowerLoaderGrabEvent(
    EntityUid PowerLoader,
    EntityUid Target,
    HashSet<EntityUid> Buckled,
    EntityUid? ToGrab = null,
    bool Handled = false
);
