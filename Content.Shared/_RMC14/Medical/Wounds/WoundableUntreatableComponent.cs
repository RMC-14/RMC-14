using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Medical.Wounds;

/// <summary>
/// For entities that should be woundable, but are unable to be treated.
/// Useful for synths, they should still bleed but be unable to be treated.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class WoundableUntreatableComponent : Component;
