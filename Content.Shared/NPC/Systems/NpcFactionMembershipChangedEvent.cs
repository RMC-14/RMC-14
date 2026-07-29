namespace Content.Shared.NPC.Systems;

/// <summary>
/// Raised after an entity's NPC faction membership has been committed and its
/// derived friendly and hostile faction caches have been refreshed.
/// </summary>
public readonly record struct NpcFactionMembershipChangedEvent;
