using Content.Shared._RMC14.IdentityManagement;
using Content.Shared.Ghost;
using Content.Shared.IdentityManagement.Components;
using Content.Shared.Whitelist;

namespace Content.Shared.IdentityManagement;

/// <summary>
///     Static content API for getting the identity entities/names for a given entity.
///     This should almost always be used in favor of metadata name, if the entity in question is a human player that
///     can have identity.
/// </summary>
public static class Identity
{
    /// <summary>
    ///     Returns the name that should be used for this entity for identity purposes.
    /// </summary>
    public static IdentityEntity Name(EntityUid uid, IEntityManager ent, EntityUid? viewer=null)
    {
        if (!uid.IsValid())
            return new IdentityEntity(uid, string.Empty);

        var meta = ent.GetComponent<MetaDataComponent>(uid);
        if (meta.EntityLifeStage <= EntityLifeStage.Initializing)
            return new IdentityEntity(uid, meta.EntityName); // Identity component and such will not yet have initialized and may throw NREs

        var uidName = meta.EntityName;

        var whitelistSystem = ent.System<EntityWhitelistSystem>();
        if (viewer != null &&
            ent.TryGetComponent(uid, out FixedIdentityComponent? fixedIdentity) &&
            fixedIdentity.Name is { } nameId &&
            whitelistSystem.IsWhitelistPass(fixedIdentity.Whitelist, viewer.Value))
        {
            var name = Loc.GetString(nameId);
            var ev = new RMCGetFixedIdentityEvent(name);
            ent.EventBus.RaiseLocalEvent(uid, ref ev);
            return new IdentityEntity(uid, ev.Name);
        }

        if (!ent.TryGetComponent<IdentityComponent>(uid, out var identity))
            return new IdentityEntity(uid, uidName);

        var ident = identity.IdentityEntitySlot.ContainedEntity;
        if (ident is null)
            return new IdentityEntity(uid, uidName);

        var identName = ent.GetComponent<MetaDataComponent>(ident.Value).EntityName;
        if (viewer == null || !CanSeeThroughIdentity(uid, viewer.Value, ent))
        {
            return new IdentityEntity(uid, identName);
        }
        if (uidName == identName)
        {
            return new IdentityEntity(uid, uidName);
        }

        return new IdentityEntity(uid, $"{uidName} ({identName})");
    }

    /// <summary>
    ///     Returns the entity that should be used for identity purposes, for example to pass into localization.
    ///     This is an extension method because of its simplicity, and if it was any harder to call it might not
    ///     be used enough for loc.
    /// </summary>
    /// <param name="viewer">
    ///     If this entity can see through identities, this method will always return the actual target entity.
    /// </param>
    public static EntityUid Entity(EntityUid uid, IEntityManager ent, EntityUid? viewer = null)
    {
        if (!ent.TryGetComponent<IdentityComponent>(uid, out var identity))
            return uid;

        if (viewer != null && CanSeeThroughIdentity(uid, viewer.Value, ent))
            return uid;

        return identity.IdentityEntitySlot.ContainedEntity ?? uid;
    }

    public static bool CanSeeThroughIdentity(EntityUid uid, EntityUid viewer, IEntityManager ent)
    {
        // Would check for uid == viewer here but I think it's better for you to see yourself
        // how everyone else will see you, otherwise people will probably get confused and think they aren't disguised
        return ent.HasComponent<GhostComponent>(viewer);
    }

}
