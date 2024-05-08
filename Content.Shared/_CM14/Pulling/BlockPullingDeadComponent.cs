using Robust.Shared.GameStates;

namespace Content.Shared._CM14.Pulling;

[RegisterComponent, NetworkedComponent]
[Access(typeof(CMPullingSystem))]
public sealed partial class BlockPullingDeadComponent : Component;
