using Content.Shared._RMC14.Weapons.Common;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Tag;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Weapons.Ranged;

public sealed class BreechLoadedSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearanceSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly SharedGunSystem _gunSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly TagSystem _tagSystem = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<BreechLoadedComponent, AttemptShootEvent>(OnAttemptShoot);
        SubscribeLocalEvent<BreechLoadedComponent, GunShotEvent>(OnGunShot);
        SubscribeLocalEvent<BreechLoadedComponent, RMCTryAmmoEjectEvent>(OnTryAmmoEject);
        SubscribeLocalEvent<BreechLoadedComponent, UniqueActionEvent>(OnUniqueAction);
        SubscribeLocalEvent<BreechLoadedComponent, InteractUsingEvent>(OnInteractUsing,
            before: new[] { typeof(SharedGunSystem) });
    }

    private void OnAttemptShoot(Entity<BreechLoadedComponent> gun, ref AttemptShootEvent args)
    {
        if (args.Cancelled || !gun.Comp.Open && (!gun.Comp.NeedOpenClose || gun.Comp.Ready))
            return;

        args.Cancelled = true;
        if (gun.Comp.Open)
            _popupSystem.PopupClient(Loc.GetString("rmc-breech-loaded-open-shoot-attempt"), args.User, args.User);
        else
            _popupSystem.PopupClient(Loc.GetString("rmc-breech-loaded-not-ready-to-shoot"), args.User, args.User);
    }

    private void OnGunShot(Entity<BreechLoadedComponent> gun, ref GunShotEvent args)
    {
        if (!gun.Comp.NeedOpenClose)
            return;

        gun.Comp.Ready = false;
        Dirty(gun);
    }

    private void OnTryAmmoEject(Entity<BreechLoadedComponent> gun, ref RMCTryAmmoEjectEvent args)
    {
        if (gun.Comp.Open)
            return;

        _popupSystem.PopupClient(Loc.GetString("rmc-breech-loaded-closed-extract-attempt"), args.User, args.User);
        args.Cancelled = true;
    }

    private void OnUniqueAction(Entity<BreechLoadedComponent> gun, ref UniqueActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        var time = _timing.CurTime;
        if (time < gun.Comp.LastToggledAt + gun.Comp.ToggleDelay)
        {
            var actionLocale = gun.Comp.Open ? Loc.GetString("rmc-breech-loaded-close") : Loc.GetString("rmc-breech-loaded-open");
            var popup = Loc.GetString("rmc-breech-loaded-toggle-attempt-cooldown", ("action", actionLocale));
            _popupSystem.PopupClient(popup, args.UserUid, args.UserUid, PopupType.Small);
            return;
        }

        gun.Comp.LastToggledAt = _timing.CurTime;
        gun.Comp.Open = !gun.Comp.Open;

        if (!gun.Comp.Open)
            gun.Comp.Ready = true;

        if (gun.Comp.ShowBreechOpen && TryComp(gun.Owner, out AppearanceComponent? appearanceComponent))
            _appearanceSystem.SetData(gun, BreechVisuals.Open, gun.Comp.Open, appearanceComponent);

        Dirty(gun);
        var sound = gun.Comp.Open ? gun.Comp.OpenSound : gun.Comp.CloseSound;
        //_audioSystem.PlayPredicted(sound, gun, args.UserUid, sound.Params);
        _audioSystem.PlayPredicted(sound, gun, args.UserUid);
    }

    private void OnInteractUsing(Entity<BreechLoadedComponent> gun, ref InteractUsingEvent args)
    {
        if (gun.Comp.Open ||
            !TryComp(gun.Owner, out BallisticAmmoProviderComponent? ammoProviderComponent) ||
            ammoProviderComponent.Whitelist == null ||
            ammoProviderComponent.Whitelist.Tags == null ||
            !_tagSystem.HasAnyTag(args.Used, ammoProviderComponent.Whitelist.Tags))
            return;

        _popupSystem.PopupClient(Loc.GetString("rmc-breech-loaded-closed-load-attempt"), args.User, args.User);
        args.Handled = true;
    }
}
