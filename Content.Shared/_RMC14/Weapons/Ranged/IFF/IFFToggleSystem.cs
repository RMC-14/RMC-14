using Content.Shared.Actions;
using Content.Shared.Popups;
using Content.Shared.Toggleable;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Audio.Systems;

namespace Content.Shared._RMC14.Weapons.Ranged.IFF;

public sealed class IFFToggleSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly RMCSelectiveFireSystem _fireSystem = default!;
    [Dependency] private readonly GunIFFSystem _iffSystem = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<IFFToggleComponent, MapInitEvent>(OnStartup, after: [typeof(RMCSelectiveFireSystem), typeof(SharedGunSystem)]);
        SubscribeLocalEvent<IFFToggleComponent, GetItemActionsEvent>(OnGetActions, after: [typeof(GunIFFSystem)]);
        SubscribeLocalEvent<IFFToggleComponent, ToggleActionEvent>(OnActionToggle, after: [typeof(GunIDLockSystem)]);
    }

    public void OnStartup(Entity<IFFToggleComponent> ent, ref MapInitEvent args)
    {
        if (!ent.Comp.ChangeStats || !TryComp<RMCSelectiveFireComponent>(ent, out var comp))
            return;

        ent.Comp.BaseFireModes = comp.BaseFireModes;
        ent.Comp.BaseModifiers = new Dictionary<SelectiveFire, SelectiveFireModifierSet>(comp.Modifiers);
        SetStats(ent);
    }

    public void SetStats(Entity<IFFToggleComponent> ent)
    {
        _fireSystem.SetModifiers(ent.Owner, ent.Comp.IFFModifiers);
        _fireSystem.SetFireModes(ent.Owner, ent.Comp.IFFFireModes, true);
        Dirty(ent);
    }

    public void ResetStats(Entity<IFFToggleComponent> ent)
    {
        _fireSystem.SetModifiers(ent.Owner, ent.Comp.BaseModifiers);
        _fireSystem.SetFireModes(ent.Owner, ent.Comp.BaseFireModes, true);
        Dirty(ent);
    }

    public void CheckStats(Entity<IFFToggleComponent> ent)
    {
        if (TryComp<RMCSelectiveFireComponent>(ent.Owner, out var comp))
        {
            if (ent.Comp.Enabled == true && comp.BaseFireModes != ent.Comp.IFFFireModes)
                SetStats(ent);
            if (ent.Comp.Enabled == false && comp.BaseFireModes != ent.Comp.BaseFireModes)
                ResetStats(ent);
        }
    }

    public void OnGetActions(Entity<IFFToggleComponent> ent, ref GetItemActionsEvent args)
    {
        if (!args.InHands)
            return;

        args.AddAction(ref ent.Comp.Action, ent.Comp.ActionID);
    }

    public void OnActionToggle(Entity<IFFToggleComponent> ent, ref ToggleActionEvent args)
    {
        if (args.Action != ent.Comp.Action)
            return;

        if (ent.Comp.RequireIDLock && TryComp<GunIDLockComponent>(ent.Owner, out var comp) && comp.Locked && comp.User != args.Performer)
        {
            var popup = Loc.GetString("rmc-id-lock-unauthorized");
            _popup.PopupClient(popup, args.Performer, args.Performer, PopupType.SmallCaution);
            return;
        }

        if (ent.Comp.Enabled)
        {
            ent.Comp.Enabled = false;
            _iffSystem.SetIFFState(ent.Owner, ent.Comp.Enabled);

            var popup = Loc.GetString("rmc-iff-toggle", ("action", Loc.GetString("rmc-iff-toggle-off")), ("gun", ent.Owner));
            _popup.PopupClient(popup, args.Performer, args.Performer, PopupType.Small);

            _audio.PlayPredicted(ent.Comp.ToggleSound, ent, args.Performer);

            if (ent.Comp.ChangeStats)
                ResetStats(ent);

            _actions.SetIcon(ent.Comp.Action.Value, ent.Comp.DisabledIcon);
        }
        else
        {
            ent.Comp.Enabled = true;
            _iffSystem.SetIFFState(ent.Owner, ent.Comp.Enabled);

            var popup = Loc.GetString("rmc-iff-toggle", ("action", Loc.GetString("rmc-iff-toggle-on")), ("gun", ent.Owner));
            _popup.PopupClient(popup, args.Performer, args.Performer, PopupType.Small);

            _audio.PlayPredicted(ent.Comp.ToggleSound, ent, args.Performer);

            if (ent.Comp.ChangeStats)
                SetStats(ent);

            _actions.SetIcon(ent.Comp.Action.Value, ent.Comp.EnabledIcon);
        }

        Dirty(ent);
    }
}
