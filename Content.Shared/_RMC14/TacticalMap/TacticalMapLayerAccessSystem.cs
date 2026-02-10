using System.Collections.Generic;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.TacticalMap;

public sealed class TacticalMapLayerAccessSystem : EntitySystem
{
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    private const string LogTag = "rmc_tacmap_layer_access";

    public override void Initialize()
    {
        SubscribeLocalEvent<TacticalMapLayerAccessComponent, GetTacticalMapLayerAccessEvent>(OnLayerAccessGet);
        SubscribeLocalEvent<InventoryComponent, GetTacticalMapLayerAccessEvent>(OnInventoryLayerAccessGet);
        SubscribeLocalEvent<HandsComponent, GetTacticalMapLayerAccessEvent>(OnHandsLayerAccessGet);
        SubscribeLocalEvent<TacticalMapLayerAccessComponent, InventoryRelayedEvent<GetTacticalMapLayerAccessEvent>>(OnItemLayerAccessGet);
    }

    private void OnLayerAccessGet(Entity<TacticalMapLayerAccessComponent> ent, ref GetTacticalMapLayerAccessEvent args)
    {
        if (ent.Comp.Layers.Count == 0)
            return;

        args.Layers.UnionWith(ent.Comp.Layers);
        Logger.InfoS(LogTag, $"Layer access from {ToPrettyString(ent.Owner)} -> [{string.Join(", ", ent.Comp.Layers)}]");
    }

    private void OnInventoryLayerAccessGet(Entity<InventoryComponent> ent, ref GetTacticalMapLayerAccessEvent args)
    {
        _inventory.RelayEvent(ent, ref args);
    }

    private void OnHandsLayerAccessGet(Entity<HandsComponent> ent, ref GetTacticalMapLayerAccessEvent args)
    {
        foreach (var held in _hands.EnumerateHeld((ent, ent)))
        {
            RaiseLocalEvent(held, ref args);
        }
    }

    private void OnItemLayerAccessGet(Entity<TacticalMapLayerAccessComponent> ent, ref InventoryRelayedEvent<GetTacticalMapLayerAccessEvent> args)
    {
        if (ent.Comp.Layers.Count == 0)
            return;

        args.Args.Layers.UnionWith(ent.Comp.Layers);
    }

    public bool TryGetLayers(
        EntityUid user,
        HashSet<ProtoId<TacticalMapLayerPrototype>> layers,
        SlotFlags slots = SlotFlags.IDCARD | SlotFlags.BELT | SlotFlags.POCKET)
    {
        layers.Clear();
        Logger.InfoS(LogTag, $"TryGetLayers for {ToPrettyString(user)} (slots={slots})");
        var ev = new GetTacticalMapLayerAccessEvent(slots, new HashSet<ProtoId<TacticalMapLayerPrototype>>());
        RaiseLocalEvent(user, ref ev);
        if (ev.Layers.Count == 0)
        {
            return false;
        }

        layers.UnionWith(ev.Layers);
        return true;
    }
}
