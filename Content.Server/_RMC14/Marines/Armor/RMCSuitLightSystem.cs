using Content.Server.Light.EntitySystems;
using Content.Shared._RMC14.Marines;
using Content.Shared._RMC14.Marines.Armor;
using Content.Shared._RMC14.Xenonids;
using Content.Shared._RMC14.Xenonids.Devour;
using Content.Shared._RMC14.Xenonids.Parasite;
using Content.Shared.Clothing;
using Content.Shared.Inventory;
using Content.Shared.Light.Components;
using Content.Shared.Mobs;
using Content.Shared.Popups;

namespace Content.Server._RMC14.Marines.Armor;

public sealed class RMCSuitLightSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly HandheldLightSystem _lights = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCSuitLightComponent, ClothingGotUnequippedEvent>(OnUnequip);
        SubscribeLocalEvent<MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<VictimInfectedComponent, ComponentStartup>(OnComponentStartup);
        SubscribeLocalEvent<MarineComponent, XenoDevouredEvent>(OnDevour);
    }

    private void OnUnequip(Entity<RMCSuitLightComponent> ent, ref ClothingGotUnequippedEvent args)
    {
        ShortLights(ent.Owner, args.Wearer);
    }

    private void OnMobStateChanged(MobStateChangedEvent args)
    {
        var uid = args.Target;

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

    private void OnDevour(Entity<MarineComponent> ent, ref XenoDevouredEvent args)
    {
        var uid = ent.Owner;
        var suit = FindSuit(uid);

        if (suit != null)
            ShortLights(suit.Value.Owner, uid);
    }

    public void ShortLights(EntityUid armor, EntityUid user)
    {
        if (TryComp(armor, out HandheldLightComponent? comp) && comp.Activated)
        {
            var ent = (armor, comp);
            _lights.TurnOff(ent);

            var popup = Loc.GetString("rmc-suit-light-short");
            _popup.PopupEntity(popup, user, user);
        }
    }

    public Entity<RMCSuitLightComponent>? FindSuit(EntityUid uid)
    {
        var slots = _inventory.GetSlotEnumerator(uid, SlotFlags.All);
        while (slots.MoveNext(out var slot))
        {
            if (TryComp(slot.ContainedEntity, out RMCSuitLightComponent? comp))
                return (slot.ContainedEntity.Value, comp);
        }

        return null;
    }
}
