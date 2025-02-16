using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Water;

[RegisterComponent, NetworkedComponent]
[Access(typeof(RMCWaterSystem))]
public sealed partial class WaterFilterComponent : Component;
