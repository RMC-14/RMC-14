using Content.Server._RMC14.Body;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Server._RMC14.Chemistry.Effects;

public sealed partial class Oxygenating : RMCChemicalEffect
{
    private static readonly ProtoId<DamageGroupPrototype> AirlossGroup = "Airloss";

    private static readonly ProtoId<DamageTypePrototype> BluntType = "Blunt";
    private static readonly ProtoId<DamageTypePrototype> PoisonType = "Poison";

    private static readonly ProtoId<ReagentPrototype> Lexorin = "RMCLexorin";

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return null;
        throw new NotImplementedException();
    }

    protected override void Tick(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var damage = new DamageSpecifier();

        if (ActualPotency >= 3)
            damage.DamageDict[AirlossGroup] = -99999f;
        else
            damage.DamageDict[AirlossGroup] = -potency;

        damageable.TryChangeDamage(args.TargetEntity, damage, true, interruptsDoAfters: false);

        var bloodstream = args.EntityManager.System<RMCBloodstreamSystem>();
        bloodstream.RemoveBloodstreamChemical(args.TargetEntity, Lexorin, potency);
    }

    protected override void TickOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var damage = new DamageSpecifier();
        damage.DamageDict[PoisonType] = potency * 0.5f;
        damageable.TryChangeDamage(args.TargetEntity, damage, true, interruptsDoAfters: false);
    }

    protected override void TickCriticalOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var damage = new DamageSpecifier();
        damage.DamageDict[BluntType] = potency;
        damage.DamageDict[PoisonType] = potency * 2f;
        damageable.TryChangeDamage(args.TargetEntity, damage, true, interruptsDoAfters: false);
    }
}
