using Robust.Shared.GameStates;

namespace Content.Shared._CM14.Xenonids.Evolution;

[RegisterComponent, NetworkedComponent]
[Access(typeof(XenoEvolutionSystem))]
public sealed partial class XenoEvolutionGranterComponent : Component;
