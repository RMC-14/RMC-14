using Content.Shared.Chemistry.Components.SolutionManager;

namespace Content.Shared._RMC14.Chemistry;

[ByRefEvent]
public readonly record struct VaporHitEvent(Entity<SolutionContainerManagerComponent> Solution);
