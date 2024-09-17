using Content.Shared._RMC14.Marines;
using Content.Shared._RMC14.Marines.Skills;
using Content.Shared.Whitelist;
using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Popups;
using Content.Shared.Timing;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared._RMC14.Stealth;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Systems;
using Content.Shared.Movement.Events;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared._RMC14.NightVision;
using Content.Shared._RMC14.Weapons.Ranged.IFF;

namespace Content.Shared._RMC14.Armor.Ghillie;

/// <summary>
/// Handles the ghillie suit's prepare position ability.
/// </summary>
public sealed class SharedGhillieSuitSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly ItemToggleSystem _toggle = default!;
    [Dependency] private readonly UseDelaySystem _useDelay = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GhillieSuitComponent, ItemToggleActivateAttemptEvent>(OnGhillieActivateAttempt);
        SubscribeLocalEvent<GhillieSuitComponent, ItemToggledEvent>(OnGhillieToggled);
        SubscribeLocalEvent<GhillieSuitComponent, GhillieSuitDoAfterEvent>(OnDoAfter);

        SubscribeLocalEvent<RMCPassiveStealthComponent, MoveInputEvent>(OnMove);
        SubscribeLocalEvent<EntityActiveInvisibleComponent, AttemptShootEvent>(OnAttemptShoot);
    }

    private void OnGhillieActivateAttempt(Entity<GhillieSuitComponent> ent, ref ItemToggleActivateAttemptEvent args)
    {
        var user = args.User;
        var comp = ent.Comp;

        if (user != null && !_whitelist.IsValid(comp.Whitelist, user))
        {
            args.Cancelled = true;

            var popup = Loc.GetString("cm-gun-unskilled", ("gun", ent.Owner));
            _popup.PopupClient(popup, user.Value, user, PopupType.SmallCaution);
            return;
        }
    }

    public void OnGhillieToggled(Entity<GhillieSuitComponent> ent, ref ItemToggledEvent args)
    {
        var suit = ent.Owner;
        var comp = ent.Comp;

        if (args.User == null)
            return;

        var user = args.User.Value;

        if (args.Activated)
        {
            var ev = new GhillieSuitDoAfterEvent();
            var doAfterEventArgs = new DoAfterArgs(EntityManager, user, comp.UseDelay, ev, ent.Owner)
            {
                BreakOnMove = true,
                BreakOnDamage = true,
                CancelDuplicate = true,
                DuplicateCondition = DuplicateConditions.SameTool
            };

            if (_doAfter.TryStartDoAfter(doAfterEventArgs))
            {
                var activatedPopupSelf = Loc.GetString("rmc-ghillie-activate-self");
                var activatedPopupOthers = Loc.GetString("rmc-ghillie-activate-others", ("user", user));
                _popup.PopupPredicted(activatedPopupSelf, activatedPopupOthers, user, user, PopupType.Medium);
            }
        }
        else
        {
            EnsureComp<RMCNightVisionVisibleComponent>(user);

            if (comp.Enabled)
            {
                var deactivatedPopupSelf = Loc.GetString("rmc-ghillie-fail-self");
                var deactivatedPopupOthers = Loc.GetString("rmc-ghillie-fail-others", ("user", user));
                _popup.PopupPredicted(deactivatedPopupSelf, deactivatedPopupOthers, user, user, PopupType.Medium);
                _useDelay.TryResetDelay(suit, id: comp.DelayId);

                comp.Enabled = false;
                Dirty(ent);
            }
        }
    }

    private void OnDoAfter(Entity<GhillieSuitComponent> ent, ref GhillieSuitDoAfterEvent args)
    {
        var user = args.User;
        var suit = ent.Owner;
        var comp = ent.Comp;

        if (args.Cancelled)
        {
            _toggle.TryDeactivate(ent.Owner, user);
            return;
        }

        if (args.Handled)
            return;

        args.Handled = true;

        var invis = EnsureComp<RMCPassiveStealthComponent>(user);
        invis.MinOpacity = comp.Opacity;
        invis.Delay = comp.InvisibilityDelay;
        invis.Enabled = false;
        Dirty(user, invis);

        EnsureComp<EntityIFFComponent>(user);

        RemCompDeferred<RMCNightVisionVisibleComponent>(user);
    }

    /// <summary>
    /// Finds a ghillie suit on a user.
    /// </summary>
    public Entity<GhillieSuitComponent>? FindSuit(EntityUid uid)
    {
        var slots = _inventory.GetSlotEnumerator(uid, SlotFlags.OUTERCLOTHING);
        while (slots.MoveNext(out var slot))
        {
            if (TryComp(slot.ContainedEntity, out GhillieSuitComponent? comp))
                return (slot.ContainedEntity.Value, comp);
        }

        return null;
    }

    private void OnMove(Entity<RMCPassiveStealthComponent> ent, ref MoveInputEvent args)
    {
        var user = ent.Owner;
        var suit = FindSuit(user);

        if (suit == null)
            return;

        _toggle.TryDeactivate(suit.Value.Owner, user);
    }

    private void OnAttemptShoot(Entity<EntityActiveInvisibleComponent> ent, ref AttemptShootEvent args)
    {
        var user = ent.Owner;
        var suit = FindSuit(user);
        var comp = ent.Comp;

        if (args.Cancelled)
            return;
        if (suit == null)
            return;

        if (_toggle.IsActivated(suit.Value.Owner))
        {
            comp.Opacity = 0;
            Dirty(user, comp);
        }
    }
}
