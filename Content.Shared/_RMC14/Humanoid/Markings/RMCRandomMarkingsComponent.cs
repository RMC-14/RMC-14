using Content.Shared.Humanoid.Markings;
using Content.Shared.Humanoid.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Humanoid.Markings;

/// <summary>
/// Applies a random marking set on mapinit for humanoids
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class RMCRandomMarkingsComponent : Component
{
    /// <summary>
    /// A list of species prototype, contains a list of marking categories with a number. The number is the chance of this category being randomized.
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<SpeciesPrototype>, Dictionary<MarkingCategories, float>> Markings;
}
