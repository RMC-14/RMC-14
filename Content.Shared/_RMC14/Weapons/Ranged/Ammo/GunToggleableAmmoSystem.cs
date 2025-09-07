using System.Runtime.InteropServices;
using Content.Shared._RMC14.Armor;
using Content.Shared._RMC14.Weapons.Common;
using Content.Shared.Actions;
using Content.Shared.Damage;
using Content.Shared.Popups;
using Content.Shared.Projectiles;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.Audio.Systems;

namespace Content.Shared._RMC14.Weapons.Ranged.Ammo;

public sealed class GunToggleableAmmoSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly CMArmorSystem _cmArmor = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    private EntityQuery<ProjectileComponent> _projectileQuery;
    private EntityQuery<CMArmorPiercingComponent> _armorPiercingQuery;

    public override void Initialize()
    {
        _projectileQuery = GetEntityQuery<ProjectileComponent>();
        _armorPiercingQuery = GetEntityQuery<CMArmorPiercingComponent>();

        SubscribeLocalEvent<GunToggleableAmmoComponent, GetItemActionsEvent>(OnGetItemActions);
        SubscribeLocalEvent<GunToggleableAmmoComponent, GunToggleAmmoActionEvent>(OnToggleAmmoAction);
        SubscribeLocalEvent<GunToggleableAmmoComponent, AmmoShotEvent>(OnAmmoShot);
        SubscribeLocalEvent<GunToggleableAmmoComponent, UniqueActionEvent>(OnUniqueAction);
    }

    private void OnGetItemActions(Entity<GunToggleableAmmoComponent> ent, ref GetItemActionsEvent args)
    {
        args.AddAction(ref ent.Comp.Action, ent.Comp.ActionId);
        Dirty(ent);
    }

    private void OnToggleAmmoAction(Entity<GunToggleableAmmoComponent> ent, ref GunToggleAmmoActionEvent args)
    {
        if (ToggleAmmo(ent, args.Performer))
            args.Handled = true;
    }

    private void OnAmmoShot(Entity<GunToggleableAmmoComponent> ent, ref AmmoShotEvent args)
    {
        var settingIndex = ent.Comp.Setting;
        if (settingIndex < 0 || settingIndex >= ent.Comp.Settings.Count)
            return;

        ref var setting = ref CollectionsMarshal.AsSpan(ent.Comp.Settings)[settingIndex];
        foreach (var projectile in args.FiredProjectiles)
        {
            if (_projectileQuery.TryComp(projectile, out var projectileComp))
            {
                projectileComp.Damage = new DamageSpecifier(setting.Damage);
                Dirty(projectile, projectileComp);
            }

            if (_armorPiercingQuery.TryComp(projectile, out var armorPiercing))
                _cmArmor.SetArmorPiercing((projectile, armorPiercing), setting.ArmorPiercing);
        }
    }

    private void OnUniqueAction(Entity<GunToggleableAmmoComponent> ent, ref UniqueActionEvent args)
    {
        if (args.Handled)
            return;

        if (ToggleAmmo(ent, args.UserUid))
            args.Handled = true;
    }

    private bool ToggleAmmo(Entity<GunToggleableAmmoComponent> ent, EntityUid user)
    {
        if (ent.Comp.Settings.Count == 0)
            return false;

        ref var settingIndex = ref ent.Comp.Setting;
        settingIndex++;
        if (settingIndex >= ent.Comp.Settings.Count)
            settingIndex = 0;

        var setting = ent.Comp.Settings[settingIndex];
        var popup = Loc.GetString("rmc-toggleable-ammo-firing", ("ammo", Loc.GetString(setting.Name)));
        _popup.PopupClient(popup, user, user, PopupType.Large);

        _audio.PlayPredicted(ent.Comp.ToggleSound, ent, user);

        if (ent.Comp.Action is { } action)
            _actions.SetIcon(action, setting.Icon);

        Dirty(ent);
        return true;
    }
}
