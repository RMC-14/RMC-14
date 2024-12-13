namespace Content.Shared._RMC14.PowerLoader;

/// <summary>
///     Raised directed on the <see cref="Used"/> entity.
/// </summary>
[ByRefEvent]
public record struct PowerLoaderInteractEvent(
    EntityUid PowerLoader,
    EntityUid Target,
    EntityUid Used,
    List<EntityUid> Buckled,
    bool Handled = false
);
