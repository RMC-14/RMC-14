using Content.Shared._CM14.Weapons.Common;
using Content.Shared.Examine;
using Content.Shared.Popups;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Audio.Systems;

namespace Content.Shared._CM14.Weapons.Ranged;

public abstract class SharedPumpActionSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<PumpActionComponent, ExaminedEvent>(OnExamined, before: [typeof(SharedGunSystem)]);
        SubscribeLocalEvent<PumpActionComponent, AttemptShootEvent>(OnAttemptShoot);
        SubscribeLocalEvent<PumpActionComponent, GunShotEvent>(OnGunShot);
        SubscribeLocalEvent<PumpActionComponent, UniqueActionEvent>(OnUniqueAction);
    }

    protected virtual void OnExamined(Entity<PumpActionComponent> ent, ref ExaminedEvent args)
    {
        // TODO CM14 the server has no idea what this keybind is supposed to be for the client
        args.PushMarkup(Loc.GetString("cm-gun-pump-examine"), 1);
    }

    protected virtual void OnAttemptShoot(Entity<PumpActionComponent> ent, ref AttemptShootEvent args)
    {
        if (!ent.Comp.Pumped)
            args.Cancelled = true;
    }

    private void OnGunShot(Entity<PumpActionComponent> ent, ref GunShotEvent args)
    {
        ent.Comp.Pumped = false;
        Dirty(ent);
    }

    private void OnUniqueAction(Entity<PumpActionComponent> ent, ref UniqueActionEvent args)
    {
        if (args.Handled)
            return;

        var ammo = new GetAmmoCountEvent();
        RaiseLocalEvent(ent.Owner, ref ammo);

        if (ammo.Count <= 0)
        {
            _popup.PopupClient(Loc.GetString("cm-gun-no-ammo-message"), args.UserUid, args.UserUid);
            args.Handled = true;
            return;
        }

        if (!ent.Comp.Running || ent.Comp.Pumped)
            return;

        ent.Comp.Pumped = true;
        Dirty(ent);

        args.Handled = true;

        _audio.PlayPredicted(ent.Comp.Sound, ent, args.UserUid);
    }
}
