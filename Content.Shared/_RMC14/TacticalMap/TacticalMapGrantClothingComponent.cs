using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.TacticalMap;

/// <summary>
/// Component for clothing items (like headsets) that grant live tactical map updates when equipped
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class TacticalMapGrantClothingComponent : Component
{
}
