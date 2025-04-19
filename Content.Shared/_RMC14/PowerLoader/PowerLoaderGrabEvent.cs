namespace Content.Shared._RMC14.PowerLoader;

[ByRefEvent]
public record struct PowerLoaderGrabEvent(
    EntityUid PowerLoader,
    EntityUid Target,
    List<EntityUid> Buckled,
    EntityUid ToGrab,
    bool Handled = false
);
