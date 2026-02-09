using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Campfire;

/// <summary>
/// Marks an item as valid fuel for campfires and braziers.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class CampfireFuelComponent : Component
{
    /// <summary>
    /// How much fuel this item provides when added to a campfire.
    /// </summary>
    [DataField]
    public int FuelAmount = 1;
}
