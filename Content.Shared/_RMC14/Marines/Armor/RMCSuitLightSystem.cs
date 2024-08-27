using Content.Shared._RMC14.Xenonids.Parasite;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Mobs;
using Content.Shared.Popups;

namespace Content.Shared._RMC14.Marines.Armor;

public sealed class RMCSuitLightSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly ItemToggleSystem _toggle = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCSuitLightComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<VictimInfectedComponent, ComponentStartup>(OnComponentStartup);
    }

    private void OnMobStateChanged(Entity<RMCSuitLightComponent> ent, ref MobStateChangedEvent args)
    {
        var uid = ent.Owner;

        if (args.NewMobState != MobState.Dead)
            return;

        var suit = FindSuit(uid);

        if (suit != null)
            ShortLights(suit.Value.Owner, uid);
    }

    private void OnComponentStartup(Entity<VictimInfectedComponent> ent, ref ComponentStartup args)
    {
        var uid = ent.Owner;
        var suit = FindSuit(uid);

        if (suit != null)
            ShortLights(suit.Value.Owner, uid);
    }


    public void ShortLights(EntityUid armor, EntityUid user)
    {
        if (_toggle.IsActivated(armor))
        {
            _toggle.TryDeactivate(armor);

            var popup = Loc.GetString("rmc-suit-light-short");
            _popup.PopupClient(popup, user);
        }
    }

    public Entity<RMCSuitLightComponent>? FindSuit(EntityUid uid)
    {
        var slots = _inventory.GetSlotEnumerator(uid, SlotFlags.OUTERCLOTHING);
        while (slots.MoveNext(out var slot))
        {
            if (TryComp(slot.ContainedEntity, out RMCSuitLightComponent? comp))
                return (slot.ContainedEntity.Value, comp);
        }

        return null;
    }
}
