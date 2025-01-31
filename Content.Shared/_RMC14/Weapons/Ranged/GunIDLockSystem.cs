using Robust.Shared.GameStates;
using Robust.Shared.Audio.Systems;
using Content.Shared.Hands;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Actions;
using Content.Shared.Toggleable;
using Content.Shared.Weapons.Ranged.Systems;
using Content.Shared.Examine;
using Content.Shared.Popups;
using Content.Shared.Interaction.Components;
using Content.Shared._RMC14.Medical.Defibrillator;
using Content.Shared.Atmos.Rotting;



namespace Content.Shared._RMC14.Weapons.Ranged;


public sealed class GunIDLockSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    public override void Initialize()
    {
        SubscribeLocalEvent<GunIDLockComponent, GotEquippedHandEvent>(OnHold);
        SubscribeLocalEvent<GunIDLockComponent, AttemptShootEvent>(OnShootAttempt);
        SubscribeLocalEvent<GunIDLockComponent, ExaminedEvent>(OnExamine);

        SubscribeLocalEvent<GunIDLockComponent, GetItemActionsEvent>(OnGetActions);
        SubscribeLocalEvent<GunIDLockComponent, ToggleActionEvent>(OnGunIDLockToggle);
    }

    private void OnHold(Entity<GunIDLockComponent> ent, ref GotEquippedHandEvent args)
    {
        CheckUserRevivability(ent);
        if (ent.Comp.User == EntityUid.Invalid)
        {
            RegisterNewUser(ent, args.User);
        }
    }
    private void OnGetActions(Entity<GunIDLockComponent> ent, ref GetItemActionsEvent args)
    {
        if (!args.InHands)
            return;
        args.AddAction(ref ent.Comp.Action, ent.Comp.ActionID);
        if (_actions.TryGetActionData(ent.Comp.Action, out var action))
        {
            Dirty(ent.Comp.Action.Value, action);
            _actions.UpdateAction(ent.Comp.Action, action);
        }
        Dirty(ent);
    }
    private void OnGunIDLockToggle(Entity<GunIDLockComponent> ent, ref ToggleActionEvent args)
    {
        if (args.Action != ent.Comp.Action)
            return;
        if (args.Performer != ent.Comp.User)
        {
            var popup = Loc.GetString("rmc-id-lock-unauthorized");
            _popup.PopupClient(popup, args.Performer, args.Performer, PopupType.SmallCaution);
            return;
        }

        if (ent.Comp.Locked)
        {
            ent.Comp.Locked = false;
            var popup = Loc.GetString("rmc-id-lock-toggle-lock", ("action", Loc.GetString("rmc-id-lock-toggle-off")), ("gun", ent.Owner));
            _popup.PopupClient(popup, args.Performer, args.Performer, PopupType.Small);
            _audio.PlayPredicted(ent.Comp.ToggleSound, ent, args.Performer);
            if (_actions.TryGetActionData(ent.Comp.Action, out var action))
            {
                action.Icon = ent.Comp.UnlockedIcon;
                Dirty(ent.Comp.Action.Value, action);
                _actions.UpdateAction(ent.Comp.Action, action);
            }
            Dirty(ent);
            return;
        }
        else
        {
            ent.Comp.Locked = true;
            var popup = Loc.GetString("rmc-id-lock-toggle-lock", ("action", Loc.GetString("rmc-id-lock-toggle-on")), ("gun", ent.Owner));
            _popup.PopupClient(popup, args.Performer, args.Performer, PopupType.Small);
            _audio.PlayPredicted(ent.Comp.ToggleSound, ent, args.Performer);
            if (_actions.TryGetActionData(ent.Comp.Action, out var action))
            {
                action.Icon = ent.Comp.LockedIcon;
                Dirty(ent.Comp.Action.Value, action);
                _actions.UpdateAction(ent.Comp.Action, action);
            }
            Dirty(ent);
            return;
        }
    }

    private void OnShootAttempt(Entity<GunIDLockComponent> ent, ref AttemptShootEvent args)
    {
        if (args.Cancelled)
            return;

        CheckUserRevivability(ent);
        if (ent.Comp.User == EntityUid.Invalid)
        {
            RegisterNewUserCombat(ent, args.User);
        }

        if (HasComp<BypassInteractionChecksComponent>(args.User))
            return;

        if (!ent.Comp.Locked)
            return;

        if (ent.Comp.User == args.User)
            return;

        args.Cancelled = true;

        var popup = Loc.GetString("rmc-shoot-id-lock-unauthorized");
        _popup.PopupClient(popup, args.User, args.User, PopupType.SmallCaution);
    }
    private void OnExamine(Entity<GunIDLockComponent> ent, ref ExaminedEvent args)
    {
        CheckUserRevivability(ent);
        if (ent.Comp.User == EntityUid.Invalid)
        {
            using (args.PushGroup(nameof(GunIDLockComponent)))
            {
                args.PushMarkup(Loc.GetString("rmc-examine-text-id-lock-no-user"));
            }
            return;
        }

        if (ent.Comp.User == args.Examiner)
        {
            if (ent.Comp.Locked)
            {
                using (args.PushGroup(nameof(GunIDLockComponent)))
                {
                    args.PushMarkup(Loc.GetString("rmc-examine-text-id-lock", ("color", Loc.GetString("rmc-id-lock-color-authorized")), ("name", ent.Comp.User)));
                }
            }
            else
            {
                using (args.PushGroup(nameof(GunIDLockComponent)))
                {
                    args.PushMarkup(Loc.GetString("rmc-examine-text-id-lock-unlocked", ("color", Loc.GetString("rmc-id-lock-color-authorized")), ("name", ent.Comp.User)));
                }
            }
        }
        else
        {
            if (ent.Comp.Locked)
            {
                using (args.PushGroup(nameof(GunIDLockComponent)))
                {
                    args.PushMarkup(Loc.GetString("rmc-examine-text-id-lock", ("color", Loc.GetString("rmc-id-lock-color-unauthorized")), ("name", ent.Comp.User)));
                }
            }
            else
            {
                using (args.PushGroup(nameof(GunIDLockComponent)))
                {
                    args.PushMarkup(Loc.GetString("rmc-examine-text-id-lock-unlocked", ("color", Loc.GetString("rmc-id-lock-color-unauthorized")), ("name", ent.Comp.User)));
                }
            }
        }
    }
    private void RegisterNewUser(Entity<GunIDLockComponent> ent, EntityUid user)
    {
        ent.Comp.User = user;
        var popup = Loc.GetString("rmc-id-lock-authorization", ("gun", ent.Owner));
        _popup.PopupClient(popup, user, PopupType.Medium);
    }
    private void RegisterNewUserCombat(Entity<GunIDLockComponent> ent, EntityUid user)
    {
        ent.Comp.User = user;
        var popup = Loc.GetString("rmc-id-lock-authorization-combat", ("gun", ent.Owner));
        _popup.PopupClient(popup, user, user, PopupType.Small);
    }

    private void ClearUser(Entity<GunIDLockComponent> ent)
    {
        ent.Comp.User = EntityUid.Invalid;
        Dirty(ent);
    }

    private void CheckUserRevivability(Entity<GunIDLockComponent> ent)
    {
        if (ent.Comp.User == EntityUid.Invalid)
            return;
        if (TerminatingOrDeleted(ent.Comp.User))
        {
            ClearUser(ent);
        }
        if (HasComp<CMDefibrillatorBlockedComponent>(ent.Comp.User))
        {
            ClearUser(ent);
        }
        if (TryComp<PerishableComponent>(ent.Comp.User, out PerishableComponent? perish) && perish is not null && perish.Stage >= 4)
        {
            ClearUser(ent);
        }
    }
}
