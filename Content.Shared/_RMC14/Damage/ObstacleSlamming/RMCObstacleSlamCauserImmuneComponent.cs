using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.Damage.ObstacleSlamming;

/// <summary>
/// Makes an entity immune to causing wall slams when an object is slammed against it.
/// </summary>
[Access(typeof(RMCObstacleSlammingSystem))]
[RegisterComponent, NetworkedComponent]
public sealed partial class RMCObstacleSlamCauserImmuneComponent : Component {}
