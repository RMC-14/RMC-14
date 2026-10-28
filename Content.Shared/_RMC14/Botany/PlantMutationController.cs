using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Botany;

/// <summary>
///     Sets named mutation controller slots in the plants seed to enable or suppress specific mutations.
/// </summary>
public sealed partial class PlantMutationController : EventEntityEffect<PlantMutationController>
{
    /// <summary>
    ///     Mutation slot names to modify.
    /// </summary>
    [DataField(required: true)]
    public List<string> Slots { get; private set; } = new();

    [DataField]
    public bool Enable { get; private set; } = true;

    /// <summary>The target value when enabling.</summary>
    [DataField]
    public float EnableValue { get; private set; } = 1f;

    /// <summary>The target value when suppressing.</summary>
    [DataField]
    public float SuppressValue { get; private set; } = -2f;

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        var slotsStr = string.Join(", ", Slots);
        return Enable
            ? Loc.GetString("reagent-effect-guidebook-plant-mutation-enable", ("slots", slotsStr))
            : Loc.GetString("reagent-effect-guidebook-plant-mutation-suppress", ("slots", slotsStr));
    }
}
