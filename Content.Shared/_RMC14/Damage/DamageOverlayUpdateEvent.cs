namespace Content.Shared._RMC14.Damage;

/// <summary>
/// Request an update of the client-side <c>DamageOverlay</c>, through <c>DamageOverlayUIController</c>.
/// </summary>
/// <remarks>
/// This needs to be broadcasted in order to be picked up by the UI controller.
/// </remarks>
/// <param name="Ent">The entity whose damage overlay should be updated.</param>
[ByRefEvent]
public readonly record struct DamageOverlayUpdateEvent(EntityUid Ent);
