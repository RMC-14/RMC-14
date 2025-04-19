using Content.Shared._RMC14.Hands;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Client.Player;
using Robust.Shared.Timing;

namespace Content.Client._RMC14.ItemPickup;

public sealed class ItemPickupSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public bool RecentItemPickUp { get; private set; }
    private TimeSpan _lastPickUp;

    public override void Initialize()
    {
        SubscribeLocalEvent<RequestStopShootEvent>(OnRequestStopShoot);
        SubscribeLocalEvent<ItemPickedUpEvent>(OnItemPickedUp);
    }

    private void OnRequestStopShoot(RequestStopShootEvent ev)
    {
        RecentItemPickUp = false;
    }

    private void OnItemPickedUp(ref ItemPickedUpEvent ev)
    {
        if (ev.User != _player.LocalEntity)
            return;

        RecentItemPickUp = true;
        _lastPickUp = _timing.CurTime;
    }

    public override void Update(float frameTime)
    {
        if (RecentItemPickUp && _timing.CurTime > _lastPickUp + TimeSpan.FromSeconds(0.15f))
            RecentItemPickUp = false;
    }
}
