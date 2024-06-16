using Robust.Shared.GameStates;

namespace Content.Shared._CM14.Dropship;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedDropshipSystem))]
public sealed partial class DropshipTerminalComponent : Component;
