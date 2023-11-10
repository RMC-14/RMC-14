using Robust.Shared.Utility;

namespace Content.Shared.CM14.Marines.Squads;

[ByRefEvent]
public readonly record struct GetMarineIconEvent(List<SpriteSpecifier> Icons);
