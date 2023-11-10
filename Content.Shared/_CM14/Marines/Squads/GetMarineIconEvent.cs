using Robust.Shared.Utility;

namespace Content.Shared._CM14.Marines.Squads;

[ByRefEvent]
public readonly record struct GetMarineIconEvent(List<SpriteSpecifier> Icons);
