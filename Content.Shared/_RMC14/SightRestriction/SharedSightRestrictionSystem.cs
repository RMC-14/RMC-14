using Content.Shared._RMC14.SightRestriction;
using Content.Shared.Actions;
using Content.Shared.Alert;
using Content.Shared.Clothing.Components;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Item;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Popups;
using Content.Shared.Rounding;
using Content.Shared.StatusEffect;
using Content.Shared.Toggleable;
using Content.Shared.Tools.Components;
using Robust.Shared.Collections;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.SightRestriction;

public sealed class SharedSightRestrictionSystem : EntitySystem
{
    [Dependency] private readonly StatusEffectsSystem _statusEffectsSystem = default!;
    [Dependency] private readonly BlindableSystem _blindingSystem = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly ClothingSystem _clothingSystem = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    // Maximum sight restriction
    private readonly SightRestrictionDefinition _maxRestrict =
        new SightRestrictionDefinition(3.0, 2.0, 1.0, 0.0);

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SightRestrictionItemComponent, GotEquippedEvent>(OnEquipped);
        SubscribeLocalEvent<SightRestrictionItemComponent, GotUnequippedEvent>(OnUnequipped);

        SubscribeLocalEvent<SightRestrictionItemComponent, ComponentStartup>(OnSightRestrictionStartup);
        SubscribeLocalEvent<SightRestrictionItemComponent, ComponentShutdown>(OnSightRestrictionShutdown);

        SubscribeLocalEvent<SightRestrictionComponent, SightRestrictionChangedEvent>(OnSightRestrictionChanged);
    }

    private void OnEquipped(Entity<SightRestrictionItemComponent> ent, ref GotEquippedEvent args)
    {
        UpdateSightRestriction(args.Equipee);
    }

    private void OnUnequipped(Entity<SightRestrictionItemComponent> ent, ref GotUnequippedEvent args)
    {
        UpdateSightRestriction(args.Equipee);
    }

    private void OnSightRestrictionStartup(Entity<SightRestrictionItemComponent> ent, ref ComponentStartup args)
    {
        var user = Transform(ent.Owner).ParentUid;
        UpdateSightRestriction(user);
    }

    private void OnSightRestrictionShutdown(Entity<SightRestrictionItemComponent> ent, ref ComponentShutdown args)
    {
        var user = Transform(ent.Owner).ParentUid;
        UpdateSightRestriction(user);
    }

    private void UpdateSightRestriction(EntityUid user)
    {
        var sightRestrict = EnsureComp<SightRestrictionComponent>(user);
        var validItems = new ValueList<EntityUid>();

        if (_inventory.TryGetContainerSlotEnumerator(user, out var slots, SlotFlags.All))
        {
            while (slots.MoveNext(out var containerSlot))
            {
                var containedEntity = containerSlot.ContainedEntity;

                if (containedEntity == null)
                    continue;

                if (!TryComp<SightRestrictionItemComponent>(containedEntity, out var restriction))
                    continue;

                AddSightRestrict(user, (containedEntity.Value, restriction));
                validItems.Add(containedEntity.Value);
            }
        }

        var toRemove = new ValueList<EntityUid>();

        foreach (var restriction in sightRestrict.Restrictions)
        {
            var item = restriction.Key;

            if (!validItems.Contains(item))
                toRemove.Add(item);
        }

        foreach (var item in toRemove)
        {
            RemoveSightRestrict((user, sightRestrict), item);
        }
    }

    private void OnSightRestrictionChanged(Entity<SightRestrictionComponent> user, ref SightRestrictionChangedEvent args)
    {
        if (user.Comp.Restrictions.Count == 0)
        {
            RemoveAllSightRestrict(user);
            return;
        }
    }

    public void AddSightRestrict(EntityUid user, Entity<SightRestrictionItemComponent> item)
    {
        var sightRestrictComp = EnsureComp<SightRestrictionComponent>(user);
        var sightRestrict = sightRestrictComp.Restrictions;

        if (!sightRestrict.ContainsKey(item.Owner))
            sightRestrict.Add(item.Owner, item.Comp.Restriction);

        var ev = new SightRestrictionChangedEvent();
        RaiseLocalEvent(user, ev);
    }

    // Removes restriction given by 'item' from 'user'
    public void RemoveSightRestrict(Entity<SightRestrictionComponent> user, EntityUid item)
    {
        var sightRestrict = user.Comp.Restrictions;

        if (sightRestrict.ContainsKey(item))
            sightRestrict.Remove(item);

        var ev = new SightRestrictionChangedEvent();
        RaiseLocalEvent(user, ev);
    }

    // Remove all sight restrictions from user
    public void RemoveAllSightRestrict(Entity<SightRestrictionComponent> user)
    {
        RemCompDeferred<SightRestrictionComponent>(user.Owner);

        var ev = new SightRestrictionRemovedEvent();
        RaiseLocalEvent(user, ev);
    }
}
