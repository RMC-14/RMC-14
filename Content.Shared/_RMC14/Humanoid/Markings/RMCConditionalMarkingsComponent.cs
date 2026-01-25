using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Humanoid.Markings;

/// <summary>
/// Used mainly for the beach bum survivor markings.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class RMCConditionalMarkingsComponent : Component
{
    [DataField]
    public Dictionary<Sex, List<ProtoId<MarkingPrototype>>> Markings;

    [DataField]
    public MarkingCategories TargetCategory;
}
