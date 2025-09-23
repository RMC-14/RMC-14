using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Webbing;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedWebbingSystem))]
public sealed partial class ClothingBlockWebbingComponent : Component;
