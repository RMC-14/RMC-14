using System.Linq;
using Content.Shared.FixedPoint;
using Content.Shared.Damage;

namespace Content.Shared._RMC14.Medical.Pain;

public sealed partial class PainSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<PainComponent, DamageChangedEvent>(OnDamageChanged);
    }

    private void OnDamageChanged(EntityUid uid, PainComponent comp, ref DamageChangedEvent args)
    {
        var damageDict = args.Damageable.Damage.DamageDict;
        UpdateCurrentPain(comp, damageDict);
        UpdateCurrentPainPercentage(comp);
        Dirty(uid, comp);
    }

    public void AddPainReductionModificator(EntityUid uid, PainReductionModificator mod, PainComponent? pain = null)
    {
        if (!Resolve(uid, ref pain))
            return;

        pain.PainReductionModificators.Add(mod);
        Dirty(uid, pain);
    }

    public FixedPoint2 GetCurrentPainPercentage(PainComponent comp)
    {
        UpdateCurrentPainPercentage(comp);
        return comp.CurrentPainPercentage;
    }

    private void UpdateCurrentPainPercentage(PainComponent comp)
    {
        UpdatePainReductionModificators(comp);
        var maxModificatorStrength = FixedPoint2.Zero;
        if (comp.PainReductionModificators.Count != 0)
        {
            maxModificatorStrength = comp.PainReductionModificators.Max(mod => mod.EffectStrength);
        }
        comp.CurrentPainPercentage = FixedPoint2.Clamp(comp.CurrentPain * comp.PainReductionDecreaceRate - maxModificatorStrength, 0, 100);
    }

    private void UpdateCurrentPain(PainComponent comp, Dictionary<string, FixedPoint2> damageDict)
    {
        var newCurrentPain = FixedPoint2.Zero;
        if (damageDict.TryGetValue("Brute", out var damage))
        {
            newCurrentPain += comp.BrutePainMultiplier * damage;
        }

        if (damageDict.TryGetValue("Burn", out damage))
        {
            newCurrentPain += comp.BurnPainMultiplier * damage;
        }

        if (damageDict.TryGetValue("Toxin", out damage))
        {
            newCurrentPain += comp.ToxinPainMultiplier * damage;
        }

        if (damageDict.TryGetValue("Airloss", out damage))
        {
            newCurrentPain += comp.AirlossPainMultiplier * damage;
        }

        comp.CurrentPain = newCurrentPain;
    }

    /// <summary>
    /// removes obsolete modifiers
    /// </summary>
    private void UpdatePainReductionModificators(PainComponent comp)
    {
        comp.PainReductionModificators = comp.PainReductionModificators.Where(mod => mod.EffectEnd >= DateTime.UtcNow).ToList();
    }
}

