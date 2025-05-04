using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.CrashLand;

[RegisterComponent, NetworkedComponent]
[Access(typeof(CrashLandSystem))]
public sealed partial class CrashLandOnTouchComponent : Component;
