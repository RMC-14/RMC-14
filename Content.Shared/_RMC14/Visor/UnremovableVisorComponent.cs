using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Visor;

[RegisterComponent, NetworkedComponent]
[Access(typeof(VisorSystem))]
public sealed partial class UnremovableVisorComponent : Component;
