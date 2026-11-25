using Content.Shared.Inventory;
using Content.Shared.NPC.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.TacticalMap;

[ByRefEvent]
public record struct TacticalMapCollectAccessEvent(SlotFlags TargetSlots) : IInventoryRelayEvent
{
    public readonly HashSet<ProtoId<NpcFactionPrototype>> Factions = new();
    public readonly HashSet<EntityUid> Hives = new();
    public readonly HashSet<EntityUid> Squads = new();
    public readonly HashSet<string> SquadGroups = new();
    public bool AllSquads;
    public bool Live;
    public bool AllChannels;
}

[ByRefEvent]
public record struct TacticalMapCanDrawEvent(EntityUid Actor, EntityUid Channel, bool Cancelled = false);

[ByRefEvent]
public record struct TacticalMapBlipActivatedEvent(EntityUid Viewer, EntityUid Actor, EntityUid Target, bool Live, bool Handled = false);

[ByRefEvent]
public record struct TacticalMapPointActivatedEvent(EntityUid Viewer, EntityUid Map, Vector2i Indices, bool Handled = false);

[ByRefEvent]
public readonly record struct TacticalMapCanvasSubmittedEvent(
    EntityUid Channel,
    EntityUid Map,
    EntityUid Actor,
    List<TacticalMapStroke> Strokes,
    Dictionary<Vector2i, TacticalMapLabelData> Labels);

[ByRefEvent]
public readonly record struct TacticalMapCollectRevealsEvent(EntityUid Map, List<TacticalMapReveal> Reveals);

public readonly record struct TacticalMapReveal(
    ProtoId<NpcFactionPrototype> From,
    ProtoId<NpcFactionPrototype> To,
    Vector2i? Center,
    float Range);

[ByRefEvent]
public record struct TacticalMapGetTrackedPositionEvent(EntityUid? Target = null);
