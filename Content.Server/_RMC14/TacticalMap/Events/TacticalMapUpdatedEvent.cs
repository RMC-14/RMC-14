using Content.Shared._RMC14.TacticalMap;

namespace Content.Server._RMC14.TacticalMap;

[ByRefEvent]
public readonly record struct TacticalMapUpdatedEvent(List<TacticalMapLine> Lines, EntityUid Actor);
