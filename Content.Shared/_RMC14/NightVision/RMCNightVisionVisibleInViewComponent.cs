using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.NightVision;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedNightVisionSystem))]
public sealed partial class RMCNightVisionVisibleInViewComponent : Component;
