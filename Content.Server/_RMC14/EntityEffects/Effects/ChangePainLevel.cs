using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;
using Content.Shared._RMC14.Medical.Pain;

namespace Content.Server._RMC14.EntityEffects.Effects;

public sealed partial class TryChangePainLevelTo : EntityEffect
{
    [DataField]
    public int Level;

    public override void Effect(EntityEffectBaseArgs args)
    {
        var painSystem = args.EntityManager.EntitySysManager.GetEntitySystem<PainSystem>();
        if (args.EntityManager.TryGetComponent(args.TargetEntity, out PainComponent? pain))
            painSystem.TryChangePainLevelTo(args.TargetEntity, Level, pain);
    }

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return null;
    }
}
