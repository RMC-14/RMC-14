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

        SubscribeLocalEvent<SightRestrictionItemComponent, ComponentStartup>(OnSightRestrictionStartup);
        SubscribeLocalEvent<SightRestrictionItemComponent, ComponentShutdown>(OnSightRestrictionShutdown);

        SubscribeLocalEvent<SightRestrictionComponent, SightRestrictionChangedEvent>(OnSightRestrictionChanged);
    }

    private void OnSightRestrictionStartup(Entity<SightRestrictionItemComponent> ent, ref ComponentStartup args)
    {
        var user = Transform(ent.Owner).ParentUid;
        if (!user.Valid)
            return;

        AddSightRestrict(user, ent);
    }

    private void OnSightRestrictionShutdown(Entity<SightRestrictionItemComponent> ent, ref ComponentShutdown args)
    {
        var user = Transform(ent.Owner).ParentUid;
        if (!user.Valid)
            return;

        if (!TryComp<SightRestrictionComponent>(user, out var restrictComp))
            return;

        RemoveSightRestrict((user, restrictComp), ent);
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
