using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;
using Content.Shared.StatusEffect;

namespace Content.Shared._RMC14.Chemistry.Conditions;

public sealed partial class HasStatusEffect : EntityEffectCondition
{
    [DataField]
    public string Key;

    [DataField]
    public bool Reversed = false;

    public override bool Condition(EntityEffectBaseArgs args)
    {
        var seSystem = args.EntityManager.EntitySysManager.GetEntitySystem<StatusEffectsSystem>();
        return Reversed ^ seSystem.HasStatusEffect(args.TargetEntity, Key);
    }

    public override string GuidebookExplanation(IPrototypeManager prototype)
    {
        return Loc.GetString("rmc14-reagent-effect-condition-guidebook-has-status-effect",
            ("key", Key),
            ("reversed", Reversed));
    }
}
