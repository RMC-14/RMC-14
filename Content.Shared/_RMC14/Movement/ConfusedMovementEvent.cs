using System.Numerics;

namespace Content.Shared._RMC14.Movement;

[ByRefEvent]
public record struct ConfusedMovementEvent(Vector2 WishDir);
