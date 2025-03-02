using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Intel.Detector;

[RegisterComponent, NetworkedComponent]
[Access(typeof(IntelDetectorSystem))]
public sealed partial class IntelDetectorTrackedComponent : Component;
