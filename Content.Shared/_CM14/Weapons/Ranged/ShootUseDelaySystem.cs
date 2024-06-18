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
        SubscribeLocalEvent<ShootUseDelayComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ShootUseDelayComponent, ShotAttemptedEvent>(OnShotAttempted);
        SubscribeLocalEvent<ShootUseDelayComponent, GunShotEvent>(OnGunShot);
    }

    private void OnMapInit(Entity<ShootUseDelayComponent> ent, ref MapInitEvent args)
    {
        _useDelay.SetLength(ent.Owner, ent.Comp.Delay, ent.Comp.DelayId);
    }

    private void OnShotAttempted(Entity<ShootUseDelayComponent> ent, ref ShotAttemptedEvent args)
    {
        var time = _timing.CurTime;
        if (TryComp(ent, out UseDelayComponent? useDelay) &&
            _useDelay.TryGetDelayInfo((ent, useDelay), out var info, ent.Comp.DelayId) &&
            info.EndTime >= time)
        {
            if (time >= ent.Comp.LastPopup + ent.Comp.PopupCooldown)
            {
                var timeLeft = info.EndTime - time;
                ent.Comp.LastPopup = _timing.CurTime;
                Dirty(ent);
                var seconds = $"{timeLeft.TotalSeconds:F1}";
                _popup.PopupClient(Loc.GetString("cm-gun-use-delay", ("seconds", seconds)), args.User, args.User);
            }

            args.Cancel();
        }
    }

    private void OnGunShot(Entity<ShootUseDelayComponent> ent, ref GunShotEvent args)
    {
        if (TryComp(ent, out UseDelayComponent? useDelay))
            _useDelay.TryResetDelay((ent, useDelay), true, ent.Comp.DelayId);
    }
}
