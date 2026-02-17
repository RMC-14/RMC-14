using System.Collections.Generic;
using Content.Shared.Inventory;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.TacticalMap;

[ByRefEvent]
public record struct GetTacticalMapLayerAccessEvent(
    SlotFlags TargetSlots,
    HashSet<ProtoId<TacticalMapLayerPrototype>> Layers
) : IInventoryRelayEvent;
