using Content.Shared.Popups;
using Content.Shared.Timing;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Timing;

namespace Content.Shared._CM14.Weapons.Ranged;

public sealed class ShootUseDelaySystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly UseDelaySystem _useDelay = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ShootUseDelayComponent, ShotAttemptedEvent>(OnShotAttempted);
        SubscribeLocalEvent<ShootUseDelayComponent, GunShotEvent>(OnGunShot);
    }

    private void OnShotAttempted(Entity<ShootUseDelayComponent> ent, ref ShotAttemptedEvent args)
    {
        if (TryComp(ent, out UseDelayComponent? useDelay) &&
            _useDelay.IsDelayed((ent, useDelay)))
        {
            var time = _timing.CurTime;
            if (time >= ent.Comp.LastPopup + ent.Comp.PopupCooldown)
            {
                var timeLeft = useDelay.DelayEndTime - time;
                ent.Comp.LastPopup = _timing.CurTime;
                Dirty(ent);
                _popup.PopupClient($"You need to wait {timeLeft.TotalSeconds:F1} seconds before shooting again!", args.User, args.User);
            }

            args.Cancel();
        }
    }

    private void OnGunShot(Entity<ShootUseDelayComponent> ent, ref GunShotEvent args)
    {
        if (TryComp(ent, out UseDelayComponent? useDelay))
            _useDelay.TryResetDelay((ent, useDelay), true);
    }
}
