using Content.Shared._RMC14.Weapons.Ranged.Battery;
using Content.Shared.Actions;
using Content.Shared.Popups;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Audio.Systems;

namespace Content.Shared._RMC14.Weapons.Ranged.Recoil;

public sealed class GunToggleableRecoilSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedGunSystem _gun = default!;
    [Dependency] private readonly RMCGunBatterySystem _gunBattery = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<GunToggleableRecoilComponent, GetItemActionsEvent>(OnGetItemActions);
        SubscribeLocalEvent<GunToggleableRecoilComponent, GunToggleRecoilActionEvent>(OnToggleRecoil);
        SubscribeLocalEvent<GunToggleableRecoilComponent, GunGetBatteryDrainEvent>(OnGetBatteryDrain);
        SubscribeLocalEvent<GunToggleableRecoilComponent, GunRefreshModifiersEvent>(OnRefreshModifiers);
        SubscribeLocalEvent<GunToggleableRecoilComponent, GunUnpoweredEvent>(OnGunUnpowered);
    }

    private void OnGetItemActions(Entity<GunToggleableRecoilComponent> ent, ref GetItemActionsEvent args)
    {
        args.AddAction(ref ent.Comp.Action, ent.Comp.ActionId);
        Dirty(ent);
    }

    private void OnToggleRecoil(Entity<GunToggleableRecoilComponent> ent, ref GunToggleRecoilActionEvent args)
    {
        args.Handled = true;
        ent.Comp.Active = !ent.Comp.Active;
        ActiveChanged(ent, args.Performer);
    }

    private void OnGetBatteryDrain(Entity<GunToggleableRecoilComponent> ent, ref GunGetBatteryDrainEvent args)
    {
        if (!ent.Comp.Active)
            return;

        args.Drain += ent.Comp.BatteryDrain;
    }

    private void OnRefreshModifiers(Entity<GunToggleableRecoilComponent> ent, ref GunRefreshModifiersEvent args)
    {
        if (!ent.Comp.Active)
            return;

        args.MinAngle = Angle.Zero;
        args.MaxAngle = Angle.Zero;
        args.CameraRecoilScalar = 0;
    }

    private void OnGunUnpowered(Entity<GunToggleableRecoilComponent> ent, ref GunUnpoweredEvent args)
    {
        if (!ent.Comp.Active)
            return;

        ent.Comp.Active = false;
        ActiveChanged(ent, null);
    }

    private void ActiveChanged(Entity<GunToggleableRecoilComponent> ent, EntityUid? user)
    {
        Dirty(ent);

        _actions.SetToggled(ent.Comp.Action, ent.Comp.Active);
        _gunBattery.RefreshBatteryDrain(ent.Owner);
        _gun.RefreshModifiers(ent.Owner);

        _audio.PlayPredicted(ent.Comp.ToggleSound, ent.Owner, user);

        if (user == null)
            return;

        var popup = ent.Comp.Active
            ? Loc.GetString("rmc-toggleable-recoil-compensation-on", ("gun", ent.Owner))
            : Loc.GetString("rmc-toggleable-recoil-compensation-off", ("gun", ent.Owner));
        _popup.PopupClient(popup, user.Value, user.Value, PopupType.Large);
    }
}
