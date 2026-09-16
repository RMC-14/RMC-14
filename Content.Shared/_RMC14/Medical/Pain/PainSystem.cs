using Content.Shared.Alert;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs.Events;
using Content.Shared.Mobs.Systems;
using Content.Shared.Rejuvenate;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using System.Linq;

namespace Content.Shared._RMC14.Medical.Pain;

public sealed partial class PainSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;

    private static readonly ProtoId<DamageGroupPrototype> BruteGroup = "Brute";
    private static readonly ProtoId<DamageGroupPrototype> BurnGroup = "Burn";
    private static readonly ProtoId<DamageGroupPrototype> ToxinGroup = "Toxin";
    private static readonly ProtoId<DamageGroupPrototype> AirlossGroup = "Airloss";

    public override void Initialize()
    {
        SubscribeLocalEvent<PainComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<PainComponent, DamageChangedEvent>(OnDamageChanged);
        SubscribeLocalEvent<PainComponent, BeforeAlertSeverityCheckEvent>(OnAlertSeverityCheck);
        SubscribeLocalEvent<PainComponent, RejuvenateEvent>(OnRejuvenate);
    }

    public void AddPainModifier(Entity<PainComponent?> ent, TimeSpan duration, FixedPoint2 effectStrength, PainModifierType type)
    {
        var expireAt = _timing.CurTime + duration;
        var mod = new PainModifier(expireAt, effectStrength, type);
        AddPainModifier(ent, mod);
    }

    public void AddPainModifier(Entity<PainComponent?> ent, PainModifier mod)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        ent.Comp.PainModifiers.Add(mod);
        DirtyField(ent, ent.Comp, nameof(PainComponent.PainModifiers));
    }

    private void OnInit(Entity<PainComponent> ent, ref ComponentInit args)
    {
        DebugTools.Assert(ent.Comp.PainLevels.SequenceEqual(ent.Comp.PainLevels.OrderBy(level => level.Threshold)),
            $"{nameof(PainComponent)}.{nameof(PainComponent.PainLevels)} entries must be written in order of their thresholds. (Low -> High)");
    }

    private void OnRejuvenate(Entity<PainComponent> ent, ref RejuvenateEvent args)
    {
        var pain = ent.Comp;
        pain.PainModifiers.Clear();
        pain.BasePain = 0;
        pain.PerceivedPain = 0;
        SetCurrentPainLevelIdx(ent, 0);
        Dirty(ent);

        _alerts.ShowAlert(ent, pain.Alert, 0);
    }

    private void OnAlertSeverityCheck(Entity<PainComponent> ent, ref BeforeAlertSeverityCheckEvent args)
    {
        if (args.CurrentAlert == ent.Comp.Alert)
        {
            args.Severity = Math.Min((short)ent.Comp.CurrentPainLevelIdx, _alerts.GetMaxSeverity(ent.Comp.Alert));
            args.CancelUpdate = true;
        }
    }

    private void OnDamageChanged(Entity<PainComponent> ent, ref DamageChangedEvent args)
    {
        var painComp = ent.Comp;
        var damage = args.Damageable.Damage;
        var newPainValue = FixedPoint2.Zero;

        newPainValue += GetDamageGroupPain(damage, BruteGroup, painComp.BrutePainMultiplier);
        newPainValue += GetDamageGroupPain(damage, BurnGroup, painComp.BurnPainMultiplier);
        newPainValue += GetDamageGroupPain(damage, ToxinGroup, painComp.ToxinPainMultiplier);
        newPainValue += GetDamageGroupPain(damage, AirlossGroup, painComp.AirlossPainMultiplier);

        if (painComp.BasePain != newPainValue)
        {
            painComp.BasePain = newPainValue;
            DirtyField(ent, ent.Comp, nameof(PainComponent.BasePain));
        }

        FixedPoint2 GetDamageGroupPain(DamageSpecifier damage, ProtoId<DamageGroupPrototype> damageGroup, FixedPoint2 painMultiplier)
        {
            if (painMultiplier != 0 &&
                _prototypes.TryIndex(damageGroup, out var groupPrototype) &&
                damage.TryGetDamageInGroup(groupPrototype, out var groupDamage))
            {
                return groupDamage * painMultiplier;
            }
            return FixedPoint2.Zero;
        }
    }

    private void UpdatePerceivedPain(Entity<PainComponent> ent)
    {
        var maxPainReductionModifierStrength = FixedPoint2.Zero;
        var painIncrease = FixedPoint2.Zero;
        foreach (var modifier in ent.Comp.PainModifiers)
        {
            switch (modifier.Type)
            {
                case PainModifierType.PainReduction:
                    maxPainReductionModifierStrength = FixedPoint2.Max(modifier.EffectStrength, maxPainReductionModifierStrength);
                    break;
                case PainModifierType.PainIncrease:
                    painIncrease += modifier.EffectStrength;
                    break;
            }
        }

        var painWithIncrease = ent.Comp.BasePain + painIncrease;
        // Pain reduction effectiveness linearly decreases as the pain goes up
        var newPainReduction = FixedPoint2.Max(0, -painWithIncrease * ent.Comp.PainReductionDecreaseRate + maxPainReductionModifierStrength);
        var newPainPercentage = FixedPoint2.Clamp(painWithIncrease - newPainReduction, 0, 100);

        if (newPainPercentage != ent.Comp.PerceivedPain)
        {
            ent.Comp.PerceivedPain = newPainPercentage;
            DirtyField(ent, ent.Comp, nameof(PainComponent.PerceivedPain));
        }
    }

    /// <summary>
    /// Setter for <see cref="PainComponent.CurrentPainLevelIdx"/>, which also raises <see cref="PainLevelChangedEvent"/>
    /// so that the client-side pain vignette damage overlay thing can update itself, and updates the health alert.
    /// </summary>
    private void SetCurrentPainLevelIdx(Entity<PainComponent> ent, int newLevel)
    {
        var painComp = ent.Comp;
        if (painComp.CurrentPainLevelIdx == newLevel)
            return;

        var previousPainLevel = painComp.CurrentPainLevelIdx;
        painComp.CurrentPainLevelIdx = newLevel;
        DirtyField(ent, painComp, nameof(PainComponent.CurrentPainLevelIdx));

        var ev = new PainLevelChangedEvent(ent, previousPainLevel, painComp.CurrentPainLevelIdx);
        RaiseLocalEvent(ent, ref ev, true);

        if (painComp.CurrentPainLevelIdx <= _alerts.GetMaxSeverity(painComp.Alert))
            _alerts.ShowAlert(ent, painComp.Alert, (short)painComp.CurrentPainLevelIdx);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var time = _timing.CurTime;
        var painQuery = EntityQueryEnumerator<PainComponent>();
        while (painQuery.MoveNext(out var uid, out var pain))
        {
            if (time < pain.NextUpdateTime || _mobState.IsDead(uid))
                continue;

            pain.NextUpdateTime = time + pain.UpdateRate;
            DirtyField(uid, pain, nameof(PainComponent.NextUpdateTime));

            if (pain.BasePain == 0 &&
                pain.PerceivedPain == 0 &&
                pain.CurrentPainLevelIdx == 0 &&
                pain.PainModifiers.Count == 0)
            {
                // Nothing to process!
                continue;
            }

            // Remove any expired modifiers.
            // (expire timings get messy on client due to the `EntityEffect` problem mentioned below, so server only here)
            if (_net.IsServer && pain.PainModifiers.RemoveAll(mod => time > mod.ExpireAt) != 0)
                DirtyField(uid, pain, nameof(PainComponent.PainModifiers));

            // Update the pain felt by the player.
            UpdatePerceivedPain((uid, pain));

            if (time >= pain.NextPainLevelUpdateTime)
            {
                pain.NextPainLevelUpdateTime = time + pain.PainLevelUpdateRate;
                DirtyField(uid, pain, nameof(PainComponent.NextPainLevelUpdateTime));

                // Get the highest level in `PainLevels` whose threshold has been passed by `PerceivedPain`.
                var highestPainLevelIdx = pain.PainLevels.FindLastIndex(level => level.Threshold <= pain.PerceivedPain);

                // Move `currentPainLevelIdx` towards `highestPainLevelIdx` by one step.
                if (highestPainLevelIdx > pain.CurrentPainLevelIdx)
                    SetCurrentPainLevelIdx((uid, pain), pain.CurrentPainLevelIdx + 1);
                else if (highestPainLevelIdx < pain.CurrentPainLevelIdx)
                    SetCurrentPainLevelIdx((uid, pain), pain.CurrentPainLevelIdx - 1);
            }

            // Server-side only from this point because `EntityEffect`s are seemingly unable to be serialized over to the client.
            if (_net.IsClient)
                continue;

            // Trigger any effects defined for this pain level.
            var currentEffectList = pain.PainLevels[pain.CurrentPainLevelIdx].LevelEffects;
            if (currentEffectList.Count == 0)
                continue;

            var args = new EntityEffectBaseArgs(uid, EntityManager);
            foreach (var effect in currentEffectList)
            {
                if (!effect.ShouldApply(args, _random))
                    continue;

                effect.Effect(args);
            }
        }
    }
}
