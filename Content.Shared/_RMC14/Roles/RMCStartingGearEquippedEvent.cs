namespace Content.Shared._RMC14.Roles;

/// <summary>
/// Raised on an entity and the item when starting gear is equipped.
/// </summary>
[ByRefEvent]
public record struct RMCStartingGearEquippedEvent(EntityUid Entity, EntityUid Item)
{
    public readonly EntityUid Entity = Entity;
    public readonly EntityUid Item = Item;
}
