using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Suicide;

[RegisterComponent, NetworkedComponent]
[Access(typeof(RMCSuicideSystem))]
public sealed partial class RMCHasSuicidedComponent : Component;
