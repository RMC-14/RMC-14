using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Areas;

[RegisterComponent, NetworkedComponent]
[Access(typeof(AreaSystem))]
public sealed partial class AreaLabelComponent : Component;
