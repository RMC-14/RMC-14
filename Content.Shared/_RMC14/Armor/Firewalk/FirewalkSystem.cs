using Content.Shared._RMC14.Aura;
using Content.Shared.Actions;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Popups;
using Content.Shared.Whitelist;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Armor.Firewalk;

public sealed class FirewalkSystem : EntitySystem
{
    [Dependency] private readonly SharedAuraSystem _aura = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly INetManager _net = default!;

    private EntityQuery<FirewalkArmorComponent> _firewalkArmorQuery;

    public override void Initialize()
    {
        base.Initialize();

        _firewalkArmorQuery = GetEntityQuery<FirewalkArmorComponent>();

        SubscribeLocalEvent<FirewalkArmorComponent, GetItemActionsEvent>(OnGetItemActions);
        SubscribeLocalEvent<FirewalkArmorComponent, FirewalkActivateActionEvent>(OnFirewalkAction);
        SubscribeLocalEvent<FirewalkArmorComponent, GotUnequippedEvent>(OnUnequipped);
    }

    private void OnGetItemActions(Entity<FirewalkArmorComponent> ent, ref GetItemActionsEvent args)
    {
        var comp = ent.Comp;

        if (args.InHands || !_inventory.InSlotWithFlags((ent, null, null), comp.Slots))
            return;

        args.AddAction(ref comp.Action, comp.ActionId);
        Dirty(ent);
    }

    private void OnFirewalkAction(Entity<FirewalkArmorComponent> ent, ref FirewalkActivateActionEvent args)
    {
        if (args.Handled)
            return;

        if (!_whitelist.IsValid(ent.Comp.Whitelist, args.Performer))
        {
            var popup = Loc.GetString("cm-gun-unskilled", ("gun", ent.Owner));
            _popup.PopupClient(popup, args.Performer, args.Performer, PopupType.SmallCaution);
            return;
        }

        EnableFirewalk(ent, args.Performer);
        args.Handled = true;
    }

    private void OnUnequipped(Entity<FirewalkArmorComponent> ent, ref GotUnequippedEvent args)
    {
        if (_timing.ApplyingState)
            return;

        if (_inventory.InSlotWithFlags((ent, null, null), ent.Comp.Slots))
            return;

        var user = args.Equipee;
        DisableFirewalk(ent, user);
    }

    public void EnableFirewalk(Entity<FirewalkArmorComponent> ent, EntityUid user)
    {
        var activeFireWalker = EnsureComp<ActiveFirewalkerComponent>(user);
        activeFireWalker.Suit = ent.Owner;
        activeFireWalker.EndAt = _timing.CurTime + ent.Comp.FirewalkTime;
        Dirty(user, activeFireWalker);

        EntityManager.AddComponents(user, ent.Comp.AddComponentsOnFirewalk);
        _aura.GiveAura(user, ent.Comp.AuraColor, ent.Comp.FirewalkTime);
        _popup.PopupClient(Loc.GetString("rmc-firewalk-activate"), user, user, PopupType.Medium);
    }

    public void DisableFirewalk(Entity<FirewalkArmorComponent> ent, EntityUid user)
    {
        RemCompDeferred<ActiveFirewalkerComponent>(user);
        RemCompDeferred<AuraComponent>(user);

        EntityManager.RemoveComponents(user, ent.Comp.AddComponentsOnFirewalk);

        if (_net.IsServer)
            _popup.PopupEntity(Loc.GetString("rmc-firewalk-end"), user, user, PopupType.Medium);
    }

    public Entity<FirewalkArmorComponent>? FindFirewalkArmor(EntityUid player)
    {
        var slots = _inventory.GetSlotEnumerator(player, SlotFlags.All);
        while (slots.MoveNext(out var slot))
        {
            if (TryComp<FirewalkArmorComponent>(slot.ContainedEntity, out var comp))
                return (slot.ContainedEntity.Value, comp);
        }

        return null;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var time = _timing.CurTime;

        var activeQuery = EntityQueryEnumerator<ActiveFirewalkerComponent>();
        while (activeQuery.MoveNext(out var uid, out var comp))
        {
            if (comp.EndAt <= time && comp.Suit is { } suit && _firewalkArmorQuery.TryComp(suit, out var suitComp))
                DisableFirewalk((suit, suitComp), uid);
        }
    }
}
