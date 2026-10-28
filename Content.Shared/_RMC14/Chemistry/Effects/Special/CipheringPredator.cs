using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Special;

public sealed partial class CipheringPredator : Ciphering // TODO RMC14: Rename / remove
{
    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "This extremely complex chemical structure represents a predator-strain biological cipher.";
    }

    // TODO RMC14: mutates a xenonid egg into a predator straing egg
}
