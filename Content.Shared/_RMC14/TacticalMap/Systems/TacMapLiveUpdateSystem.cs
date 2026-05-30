using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.TacticalMap;

public sealed class TacMapLiveUpdateSystem : EntitySystem
{
    [Dependency] private readonly InventorySystem _inv = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<TacticalMapUserComponent, ComponentStartup>(OnTacticalMapUserStartup);
        SubscribeLocalEvent<GrantTacMapLiveUpdateComponent, GotEquippedEvent>(OnGotEquipped);
        SubscribeLocalEvent<GrantTacMapLiveUpdateComponent, GotUnequippedEvent>(OnGotUnequipped);
    }

    private void OnTacticalMapUserStartup(Entity<TacticalMapUserComponent> ent, ref ComponentStartup args)
    {
        RefreshLiveUpdate(ent);
    }

    private void RefreshLiveUpdate(Entity<TacticalMapUserComponent> ent)
    {
        if (_timing.ApplyingState)
            return;

        // before user component exists maybe
        if (!_inv.TryGetInventoryEntity<GrantTacMapLiveUpdateComponent>(ent.Owner, out _))
            return;

        if (ent.Comp.LiveUpdate)
            return;

        ent.Comp.LiveUpdate = true;
        Dirty(ent);
    }

    private void OnGotEquipped(Entity<GrantTacMapLiveUpdateComponent> ent, ref GotEquippedEvent args)
    {
        if (_timing.ApplyingState)
            return;

        if ((ent.Comp.Slots & args.SlotFlags) == 0)
            return;

        var user = EnsureComp<TacticalMapUserComponent>(args.Equipee);
        if (user.LiveUpdate)
            return;

        user.LiveUpdate = true;
        Dirty(args.Equipee, user);
    }

    private void OnGotUnequipped(Entity<GrantTacMapLiveUpdateComponent> ent, ref GotUnequippedEvent args)
    {
        if (_timing.ApplyingState)
            return;

        if ((ent.Comp.Slots & args.SlotFlags) == 0)
            return;

        if (_inv.TryGetInventoryEntity<GrantTacMapLiveUpdateComponent>(args.Equipee, out _))
            return;

        // ovi live update has own system
        if (HasComp<TacticalMapLiveUpdateOnOviComponent>(args.Equipee))
            return;

        if (!TryComp(args.Equipee, out TacticalMapUserComponent? user) || !user.LiveUpdate)
            return;

        user.LiveUpdate = false;
        Dirty(args.Equipee, user);
    }
}
