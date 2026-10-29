using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Positive;

public sealed partial class Explosive : RMCChemicalEffect
{
    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "The chemical is highly explosive. Do not ignite. Careful when handling, sensitivity is based off the OD threshold, which can lead to spontaneous detonation.";
    }

    // TODO RMC14: cm13 also runs handle_volatiles() for custom chemical grenades, mortar shells, rockets &
    // plastic explosives filled with reagents
    public float PowerDelta => Potency;

    public override bool IsVolatile => true;

    public float FalloffDelta => -3f / MathF.Max((float) Potency, 0.01f);
}
