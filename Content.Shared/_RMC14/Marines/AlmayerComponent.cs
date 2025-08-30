using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Marines;

[RegisterComponent, NetworkedComponent]
[Access(typeof(WarshipSystem))]
public sealed partial class AlmayerComponent : Component;
