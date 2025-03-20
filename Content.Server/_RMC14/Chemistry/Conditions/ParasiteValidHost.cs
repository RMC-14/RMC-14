using Content.Shared._RMC14.Xenonids.Parasite;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Server._RMC14.Chemistry.Conditions;
public sealed partial class ParasiteValidHost : EntityEffectCondition
{
    [DataField]
    public bool Valid = true;

    public override bool Condition(EntityEffectBaseArgs args)
    {
        return args.EntityManager.HasComponent<InfectableComponent>(args.TargetEntity) == Valid;
    }

    public override string GuidebookExplanation(IPrototypeManager prototype)
    {
        return Loc.GetString("reagent-effect-condition-guidebook-valid-host", ("valid", Valid));
    }
}
