using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.Eye;

[RegisterComponent, NetworkedComponent]
[Access(typeof(QueenEyeSystem))]
public sealed partial class QueenEyeComponent : Component;
