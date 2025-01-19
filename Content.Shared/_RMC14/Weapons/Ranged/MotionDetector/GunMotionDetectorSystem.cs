using Content.Shared._RMC14.MotionDetector;
using Content.Shared._RMC14.Weapons.Ranged.Battery;
using Content.Shared.Actions;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;

namespace Content.Shared._RMC14.Weapons.Ranged.MotionDetector;

public sealed class GunMotionDetectorSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly MotionDetectorSystem _motionDetector = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly RMCGunBatterySystem _rmcGunBattery = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<GunToggleableMotionDetectorComponent, GetItemActionsEvent>(OnGetItemActions);
        SubscribeLocalEvent<GunToggleableMotionDetectorComponent, GunToggleableMotionDetectorActionEvent>(OnToggleAction);
        SubscribeLocalEvent<GunToggleableMotionDetectorComponent, GunGetBatteryDrainEvent>(OnGetBatteryDrain);
        SubscribeLocalEvent<GunToggleableMotionDetectorComponent, GunUnpoweredEvent>(OnGunUnpowered);
        SubscribeLocalEvent<GunToggleableMotionDetectorComponent, MotionDetectorUpdatedEvent>(OnMotionDetectorUpdated);
    }

    private void OnGetItemActions(Entity<GunToggleableMotionDetectorComponent> ent, ref GetItemActionsEvent args)
    {
        args.AddAction(ref ent.Comp.Action, ent.Comp.ActionId);
        Dirty(ent);
    }

    private void OnToggleAction(Entity<GunToggleableMotionDetectorComponent> ent, ref GunToggleableMotionDetectorActionEvent args)
    {
        var user = args.Performer;
        if (TryComp(ent, out MotionDetectorComponent? detector))
            _motionDetector.Toggle((ent, detector));

        _audio.PlayPredicted(ent.Comp.ToggleSound, ent, user);
        DetectorUpdated(ent);

        var popup = _motionDetector.IsEnabled((ent, detector))
            ? Loc.GetString("rmc-toggleable-motion-detector-on", ("gun", ent))
            : Loc.GetString("rmc-toggleable-motion-detector-off", ("gun", ent));
        _popup.PopupClient(popup, user, user, PopupType.Large);
    }

    private void OnGetBatteryDrain(Entity<GunToggleableMotionDetectorComponent> ent, ref GunGetBatteryDrainEvent args)
    {
        if (_motionDetector.IsEnabled(ent.Owner))
            args.Drain += ent.Comp.BatteryDrain;
    }

    private void OnGunUnpowered(Entity<GunToggleableMotionDetectorComponent> ent, ref GunUnpoweredEvent args)
    {
        if (!TryComp(ent, out MotionDetectorComponent? detector))
            return;

        _motionDetector.Disable((ent, detector));
        DetectorUpdated(ent);
    }

    private void OnMotionDetectorUpdated(Entity<GunToggleableMotionDetectorComponent> ent, ref MotionDetectorUpdatedEvent args)
    {
        DetectorUpdated(ent);
    }

    private void DetectorUpdated(Entity<GunToggleableMotionDetectorComponent> ent)
    {
        var enabled = false;
        if (TryComp(ent, out MotionDetectorComponent? detector))
            enabled = _motionDetector.IsEnabled((ent, detector));

        _rmcGunBattery.RefreshBatteryDrain(ent.Owner);
        _actions.SetToggled(ent.Comp.Action, enabled);
    }
}
