using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.JoinXeno;

[RegisterComponent, NetworkedComponent]
[Access(typeof(LarvaQueueSystem))]
public sealed partial class CanBeLarvaQueuedComponent : Component;
