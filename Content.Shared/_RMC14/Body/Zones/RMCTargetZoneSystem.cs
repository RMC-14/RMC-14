using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Body.Zones;

/// <summary>
///     tracks which body zone each player is aiming at.
///     the zone is not sent with every attack. The client sends it once when it changes,
///     the server stores it on the player's entity, and anything that needs it calls
/// </summary>
public sealed class RMCTargetZoneSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;

    public override void Initialize()
    {
        SubscribeAllEvent<RMCTargetZoneSelectEvent>(OnSelect);
    }

    private void OnSelect(RMCTargetZoneSelectEvent args, EntitySessionEventArgs session)
    {
        // only ever set the zone on the sender's own entity, never one named by the client
        if (session.SenderSession.AttachedEntity is not { } user)
            return;

        SetZone(user, args.Zone);
    }

    /// <summary>
    ///     the zone this entity is aiming at, chest if it can't aim
    /// </summary>
    public RMCTargetZone GetZone(Entity<RMCTargetZoneComponent?> ent)
    {
        return Resolve(ent, ref ent.Comp, false) ? ent.Comp.Selected : RMCTargetZone.Chest;
    }

    /// <summary>
    ///     called by the client to select a zone for its own entity, predicted
    /// </summary>
    public void RequestZone(RMCTargetZone zone)
    {
        if (_net.IsClient)
            RaisePredictiveEvent(new RMCTargetZoneSelectEvent(zone));
    }

    /// <summary>
    ///     sets the zone directly. rejects values outside the enum, since the zone comes from the client
    ///     /// </summary>
    public bool SetZone(Entity<RMCTargetZoneComponent?> ent, RMCTargetZone zone)
    {
        if (!Enum.IsDefined(zone) ||
            !Resolve(ent, ref ent.Comp, false) ||
            ent.Comp.Selected == zone)
        {
            return false;
        }

        var old = ent.Comp.Selected;
        ent.Comp.Selected = zone;
        Dirty(ent);

        var ev = new RMCTargetZoneChangedEvent(old, zone);
        RaiseLocalEvent(ent, ref ev);
        return true;
    }
}

[Serializable, NetSerializable]
public sealed class RMCTargetZoneSelectEvent(RMCTargetZone zone) : EntityEventArgs
{
    public readonly RMCTargetZone Zone = zone;
}

[ByRefEvent]
public readonly record struct RMCTargetZoneChangedEvent(RMCTargetZone Old, RMCTargetZone New);
