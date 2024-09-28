using Content.Shared._RMC14.Marines.Squads;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.Marines;

public abstract class SharedMarineSystem : EntitySystem
{
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MarineComponent, GetMarineIconEvent>(OnMarineGetIcon);

        SubscribeLocalEvent<GrantMarineIconsComponent, GotEquippedEvent>(OnGotEquipped);
        SubscribeLocalEvent<GrantMarineIconsComponent, GotUnequippedEvent>(OnGotUnequipped);
    }

    private void OnGotEquipped(Entity<GrantMarineIconsComponent> ent, ref GotEquippedEvent args)
    {
        if (_timing.ApplyingState)
            return;

        if ((ent.Comp.Slots & args.SlotFlags) == 0)
            return;

        EnsureComp<ShowMarineIconsComponent>(args.Equipee);
    }

    private void OnGotUnequipped(Entity<GrantMarineIconsComponent> ent, ref GotUnequippedEvent args)
    {
        if (_timing.ApplyingState)
            return;

        if ((ent.Comp.Slots & args.SlotFlags) == 0)
            return;

        if (!_inventory.TryGetInventoryEntity<GrantMarineIconsComponent>(args.Equipee, out _))
            RemCompDeferred<ShowMarineIconsComponent>(args.Equipee);
    }

    private void OnMarineGetIcon(Entity<MarineComponent> marine, ref GetMarineIconEvent args)
    {
        if (marine.Comp.Icon is { } icon)
            args.Icon = icon;
    }

    public GetMarineIconEvent GetMarineIcon(EntityUid uid)
    {
        var ev = new GetMarineIconEvent();
        RaiseLocalEvent(uid, ref ev);
        return ev;
    }

    public void MakeMarine(EntityUid uid, SpriteSpecifier? icon)
    {
        var marine = EnsureComp<MarineComponent>(uid);
        marine.Icon = icon;
        Dirty(uid, marine);
    }
}
