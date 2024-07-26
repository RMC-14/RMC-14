using Content.Shared._RMC14.Marines;
using Content.Shared._RMC14.Marines.Skills;
using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Popups;
using Content.Shared.Timing;
using Content.Shared.Weapons.Ranged.Events;

namespace Content.Shared._RMC14.Armor.Ghillie;

/// <summary>
/// Handles the ghillie suit's prepare position ability.
/// </summary>
public sealed class SharedGhillieSuitSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SkillsSystem _skills = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GhillieSuitComponent, GetItemActionsEvent>(OnGetItemActions);
        SubscribeLocalEvent<GhillieSuitComponent, GhillieActionEvent>(OnGhillieAction);
        SubscribeLocalEvent<GhillieSuitComponent, GotUnequippedEvent>(OnUnequipped);
        SubscribeLocalEvent<GhillieSuitComponent, GhillieSuitDoAfterEvent>(OnDoAfter);

        SubscribeLocalEvent<MarineComponent, MoveEvent>(OnMove);
    }

    /// <summary>
    /// Only add the action if the user has sufficient skills
    /// </summary>
    private void OnGetItemActions(Entity<GhillieSuitComponent> ent, ref GetItemActionsEvent args)
    {
        var comp = ent.Comp;

        if (args.InHands || !_inventory.InSlotWithFlags((ent, null, null), SlotFlags.OUTERCLOTHING))
            return;
        if (!_skills.HasSkills(args.User, in comp.SkillRequired))
            return;

        args.AddAction(ref comp.Action, comp.ActionId);
        Dirty(ent);
    }

    private void OnGhillieAction(Entity<GhillieSuitComponent> ent, ref GhillieActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        var user = args.Performer;
        var comp = ent.Comp;

        if (!comp.Enabled)
        {
            var ev = new GhillieSuitDoAfterEvent();
            var doAfterEventArgs = new DoAfterArgs(EntityManager, user, comp.CloakDelay, ev, ent.Owner)
            {
                BreakOnMove = true,
                BreakOnDamage = true,
                AttemptFrequency = AttemptFrequency.EveryTick,
                CancelDuplicate = true,
                DuplicateCondition = DuplicateConditions.None
            };

            if (_doAfter.TryStartDoAfter(doAfterEventArgs))
            {
                var activatedPopupSelf = Loc.GetString("rmc-ghillie-activate-self");
                var activatedPopupOthers = Loc.GetString("rmc-ghillie-activate-others", ("user", user));
                _popup.PopupPredicted(activatedPopupSelf, activatedPopupOthers, user, user, PopupType.Medium);
            }
        }
        else
            SetCloakEnabled(ent, user, false);

        Dirty(ent);
    }

    private void OnDoAfter(Entity<GhillieSuitComponent> ent, ref GhillieSuitDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        args.Handled = true;

        SetCloakEnabled(ent, args.User, true);
        Dirty(ent);
    }

    /// <summary>
    /// Disable the abilities when the suit unequipped
    /// </summary>
    private void OnUnequipped(Entity<GhillieSuitComponent> ent, ref GotUnequippedEvent args)
    {
        var user = args.Equipee;

        if (_inventory.InSlotWithFlags((ent, null, null), SlotFlags.OUTERCLOTHING))
            return;

        SetCloakEnabled(ent, user, false);
    }

    /// <summary>
    /// Cloaks or uncloaks the user.
    /// </summary>
    public void SetCloakEnabled(Entity<GhillieSuitComponent> ent, EntityUid user, bool enable)
    {
        var comp = ent.Comp;

        if (enable)
            EntityManager.AddComponents(user, comp.Components);
        else
            EntityManager.RemoveComponents(user, comp.Components);

        comp.Enabled = enable;
        Dirty(ent);
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

    private void OnMove(Entity<MarineComponent> ent, ref MoveEvent args)
    {
        var user = ent.Owner;
        var suit = FindSuit(user);
        var length = (args.NewPosition.Position - args.OldPosition.Position).Length();

        if (suit != null && length > 0)
            SetCloakEnabled(suit.Value, user, false);
    }
}
