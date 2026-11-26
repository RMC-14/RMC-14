using Content.Shared._RMC14.TacticalMap;
using Content.Shared.Inventory.Events;
using Content.Shared.NPC.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server._RMC14.TacticalMap;

public sealed partial class TacticalMapSystem
{
    private void InitializeGrant()
    {
        SubscribeLocalEvent<GrantTacticalMapComponent, GotEquippedEvent>(OnGrantEquipped);
        SubscribeLocalEvent<GrantTacticalMapComponent, GotUnequippedEvent>(OnGrantUnequipped);
        SubscribeLocalEvent<TacticalMapUserComponent, ComponentShutdown>(OnUserShutdown);
    }

    private void OnGrantEquipped(Entity<GrantTacticalMapComponent> ent, ref GotEquippedEvent args)
    {
        if ((ent.Comp.Slots & args.SlotFlags) == 0)
            return;

        var wearer = args.Equipee;
        var granted = EnsureComp<TacticalMapGrantedComponent>(wearer);
        if (ent.Comp.GrantMap && !HasComp<TacticalMapUserComponent>(wearer))
        {
            AddComp(wearer, new TacticalMapUserComponent { ActionId = ent.Comp.ActionId });
            granted.User = true;
        }

        if (!HasComp<TacticalMapTrackedComponent>(wearer))
        {
            AddComp(wearer, new TacticalMapTrackedComponent
            {
                TrackDead = true,
                Factions = new List<ProtoId<NpcFactionPrototype>>(ent.Comp.Factions),
            });
            granted.Tracked = true;
        }
    }

    private void OnGrantUnequipped(Entity<GrantTacticalMapComponent> ent, ref GotUnequippedEvent args)
    {
        if ((ent.Comp.Slots & args.SlotFlags) == 0)
            return;

        var wearer = args.Equipee;
        if (_inventory.TryGetInventoryEntity<GrantTacticalMapComponent>(wearer, out _) ||
            !TryComp(wearer, out TacticalMapGrantedComponent? granted))
        {
            return;
        }

        if (granted.User)
            RemComp<TacticalMapUserComponent>(wearer);

        if (granted.Tracked)
        {
            RemComp<ActiveTacticalMapTrackedComponent>(wearer);
            RemComp<TacticalMapTrackedComponent>(wearer);
        }

        RemComp<TacticalMapGrantedComponent>(wearer);
    }

    private void OnUserShutdown(Entity<TacticalMapUserComponent> ent, ref ComponentShutdown args)
    {
        _selfTracked.Remove(ent);
        if (TerminatingOrDeleted(ent))
            return;

        UI.CloseUi(ent.Owner, TacticalMapUserUi.Key);
        if (ent.Comp.Action is { } action)
            _actions.RemoveAction(ent.Owner, action);
    }
}
