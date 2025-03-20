using Content.Shared._RMC14.Xenonids.Parasite;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Server._RMC14.Chemistry.Conditions;
public sealed partial class ParasiteStatus : EntityEffectCondition
{
    [DataField(required: true)]
    public bool Infected = default!;

    public override bool Condition(EntityEffectBaseArgs args)
    {
        return args.EntityManager.HasComponent<VictimInfectedComponent>(args.TargetEntity) == Infected;
    }

    public override string GuidebookExplanation(IPrototypeManager prototype)
    {
        return Loc.GetString("reagent-effect-condition-guidebook-infected", ("infected", Infected));
    }
}
