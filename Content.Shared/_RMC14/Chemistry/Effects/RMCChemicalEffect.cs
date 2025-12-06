using System.Diagnostics.CodeAnalysis;
using Content.Shared._RMC14.Marines;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared._RMC14.Chemistry.Effects;

public abstract partial class RMCChemicalEffect : EntityEffect
{
    protected static readonly ProtoId<DamageGroupPrototype> BruteGroup = "Brute";
    protected static readonly ProtoId<DamageTypePrototype> BluntType = "Blunt";

    protected static readonly ProtoId<DamageGroupPrototype> BurnGroup = "Burn";
    protected static readonly ProtoId<DamageTypePrototype> HeatType = "Heat";
    protected static readonly ProtoId<DamageTypePrototype> CausticType = "Caustic";

    protected static readonly ProtoId<DamageGroupPrototype> ToxinGroup = "Toxin";
    protected static readonly ProtoId<DamageTypePrototype> PoisonType = "Poison";

    protected static readonly ProtoId<DamageGroupPrototype> AirlossGroup = "Airloss";
    protected static readonly ProtoId<DamageTypePrototype> AsphyxiationType = "Asphyxiation";

    protected static readonly ProtoId<DamageGroupPrototype> GeneticGroup = "Genetic";
    protected static readonly ProtoId<DamageTypePrototype> GeneticType = "Cellular";

    public abstract string Abbreviation { get; }

    public virtual int? MaxLevel { get; }

    [DataField]
    public float Level;

    private float _moddedPotency;

    /// <summary>
    ///     The value that should be used in actual calculations for chemical effect
    ///     MOST OF THE TIME
    ///     Halved since level is halved before being used
    /// </summary>
    public float Potency => (_moddedPotency != 0 ? _moddedPotency : Level) * 0.5f;

    // Halved again since chemicals tick every second in SS14, not every 2
    public float PotencyPerSecond => Potency * 0.5f;

    [DataField]
    public float NutFactor;
    [DataField]
    public float NutMetabolism;

    public float NutrimentFactor => NutFactor * NutMetabolism;

    public override void Effect(EntityEffectBaseArgs args)
    {
        if (args is not EntityEffectReagentArgs { Reagent: { } reagent } reagentArgs)
            return;

        var damageable = args.EntityManager.System<DamageableSystem>();
        var scale = reagentArgs.Scale;
        var boost = CalculateReagentBoost(reagentArgs);
        _moddedPotency = (Level + boost) * GetEffectiveness(args.TargetEntity).Float();
        var scaledPotency = PotencyPerSecond * scale;
        Tick(damageable, scaledPotency, reagentArgs);

        var totalQuantity = FixedPoint2.Zero;
        if (reagentArgs.Source != null)
            totalQuantity = reagentArgs.Source.GetTotalPrototypeQuantity(reagent.ID);

        if (reagent.Overdose != null && totalQuantity >= reagent.Overdose)
            TickOverdose(damageable, scaledPotency, reagentArgs);

        if (reagent.CriticalOverdose != null && totalQuantity >= reagent.CriticalOverdose)
            TickCriticalOverdose(damageable, scaledPotency, reagentArgs);
    }

    private static float CalculateReagentBoost(EntityEffectReagentArgs args)
    {
        var boost = 0f;
        if (args.Reagent?.Metabolisms == null)
            return boost;

        foreach (var (_, entry) in args.Reagent.Metabolisms)
        {
            foreach (var effect in entry.Effects)
            {
                if (effect is RMCChemicalEffect rmcEffect)
                {
                    rmcEffect.ReagentBoost(args, ref boost);
                }
            }
        }

        return boost;
    }

    protected virtual void ReagentBoost(EntityEffectReagentArgs args, ref float boost)
    {
    }

    public virtual bool CanBeIngested()
    {
        return true;
    }

    public virtual bool CanMetabolize(EntityUid target)
    {
        return true;
    }

    public virtual FixedPoint2 GetMetabolismModifier()
    {
        return FixedPoint2.New(1);
    }

    public virtual FixedPoint2 GetEffectiveness(EntityUid target)
    {
        return FixedPoint2.New(1);
    }

    protected virtual void Tick(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
    }

    protected virtual void TickOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
    }

    protected virtual void TickCriticalOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
    }

    protected T System<T>(EntityEffectReagentArgs args) where T : IEntitySystem
    {
        return args.EntityManager.System<T>();
    }

    protected bool TryComp<T>(EntityEffectReagentArgs args, [NotNullWhen(true)] out T? comp) where T : IComponent
    {
        return args.EntityManager.TryGetComponent(args.TargetEntity, out comp);
    }

    protected T? CompOrNull<T>(EntityEffectReagentArgs args) where T : IComponent
    {
        return args.EntityManager.GetComponentOrNull<T>(args.TargetEntity);
    }

    protected T EnsureComp<T>(EntityEffectReagentArgs args) where T : IComponent, new()
    {
        return args.EntityManager.EnsureComponent<T>(args.TargetEntity);
    }

    protected void TryChangeDamage(EntityEffectReagentArgs args, ProtoId<DamageTypePrototype> type, FixedPoint2 amount)
    {
        var damageable = args.EntityManager.System<DamageableSystem>();
        var damage = new DamageSpecifier();
        damage.DamageDict[type] = amount;
        damageable.TryChangeDamage(args.TargetEntity, damage, ignoreResistances: true, interruptsDoAfters: false);
    }

    protected bool IsHumanoid(EntityEffectReagentArgs args)
    {
        // TODO RMC14
        return args.EntityManager.HasComponent<MarineComponent>(args.TargetEntity);
    }

    protected bool ProbHundred(float prob)
    {
        prob /= 100;
        prob = Math.Clamp(prob, 0, 1);
        return IoCManager.Resolve<IRobustRandom>().Prob(prob);
    }

    protected bool ProbHundred(FixedPoint2 prob)
    {
        return ProbHundred(prob.Float());
    }
}
