using Content.Shared.Movement.Systems;

namespace Content.Shared._RMC14.Movement;

[ByRefEvent]
public readonly record struct RMCLateRefreshMovementSpeedModifiersEvent(RefreshMovementSpeedModifiersEvent Modifiers);
