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

    private void OnInit(Entity<PainComponent> ent, ref ComponentInit args)
    {
        DebugTools.Assert(ent.Comp.PainLevels.SequenceEqual(ent.Comp.PainLevels.OrderBy(level => level.Threshold)),
            $"{nameof(PainComponent)}.{nameof(PainComponent.PainLevels)} entries must be written in order of their thresholds. (Low -> High)");
    }

    private void OnRejuvenate(Entity<PainComponent> ent, ref RejuvenateEvent args)
    {
        var pain = ent.Comp;
        pain.PainModifiers.Clear();
        pain.CurrentPain = 0;
        pain.CurrentPainPercentage = 0;
        SetCurrentPainLevel(ent, 0);
        Dirty(ent);

        _alerts.ShowAlert(ent, pain.Alert, 0);
    }

    private void OnAlertSeverityCheck(Entity<PainComponent> ent, ref BeforeAlertSeverityCheckEvent args)
    {
        if (args.CurrentAlert == ent.Comp.Alert)
        {
            args.Severity = Math.Min((short)ent.Comp.CurrentPainLevel, _alerts.GetMaxSeverity(ent.Comp.Alert));
            args.CancelUpdate = true;
        }
    }

    private void OnDamageChanged(Entity<PainComponent> ent, ref DamageChangedEvent args)
    {
        UpdateCurrentPain(ent, args.Damageable.Damage);
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
        UpdateCurrentPainPercentage((ent, ent.Comp));
    }

    private void UpdateCurrentPain(Entity<PainComponent> ent, DamageSpecifier damage)
    {
        var painComp = ent.Comp;
        var newPainValue = FixedPoint2.Zero;

        newPainValue += GetDamageGroupPain(damage, BruteGroup, painComp.BrutePainMultiplier);
        newPainValue += GetDamageGroupPain(damage, BurnGroup, painComp.BurnPainMultiplier);
        newPainValue += GetDamageGroupPain(damage, ToxinGroup, painComp.ToxinPainMultiplier);
        newPainValue += GetDamageGroupPain(damage, AirlossGroup, painComp.AirlossPainMultiplier);

        if (painComp.CurrentPain != newPainValue)
        {
            painComp.CurrentPain = newPainValue;
            DirtyField(ent, ent.Comp, nameof(PainComponent.CurrentPain));
        }
    }

    private void UpdateCurrentPainPercentage(Entity<PainComponent> ent)
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

        var realCurrentPain = ent.Comp.CurrentPain + painIncrease;
        // Pain reduction effectiveness linearly decreases as the pain goes up
        var newPainReduction = FixedPoint2.Max(0, -realCurrentPain * ent.Comp.PainReductionDecreaseRate + maxPainReductionModifierStrength);
        var newPainPercentage = FixedPoint2.Clamp(realCurrentPain - newPainReduction, 0, 100);

        if (newPainPercentage != ent.Comp.CurrentPainPercentage)
        {
            ent.Comp.CurrentPainPercentage = newPainPercentage;
            DirtyField(ent, ent.Comp, nameof(PainComponent.CurrentPainPercentage));
        }
    }

    /// <summary>
    /// Move <paramref name="ent"/>'s <see cref="PainComponent.CurrentPainLevel"/> towards <paramref name="targetLevel"/> by one level at a time.
    /// </summary>
    /// <example>
    /// Before: ent.Comp.CurrentPainLevel == 2
    /// <code>
    ///     StepCurrentPainLevel(ent, 5);
    /// </code>
    /// After: ent.Comp.CurrentPainLevel == 3
    /// </example>
    /// <param name="ent">Entity whose <see cref="PainComponent"/> should be modified.</param>
    /// <param name="targetLevel">Target pain level to move towards.</param>
    private void StepCurrentPainLevel(Entity<PainComponent> ent, int targetLevel)
    {
        var painComp = ent.Comp;

        if (targetLevel == painComp.CurrentPainLevel)
            return;

        // `CompareTo()` returns either 1, 0, or -1, so this modifies it one step at a time.
        SetCurrentPainLevel(ent, painComp.CurrentPainLevel + targetLevel.CompareTo(painComp.CurrentPainLevel));

        if (painComp.CurrentPainLevel <= _alerts.GetMaxSeverity(painComp.Alert))
            _alerts.ShowAlert(ent, painComp.Alert, (short)painComp.CurrentPainLevel);
    }

    /// <summary>
    /// Simple setter for <see cref="PainComponent.CurrentPainLevel"/> which also raises <see cref="PainLevelChangedEvent"/>
    /// so that the client-side pain vignette damage overlay thing can update itself.
    /// </summary>
    private void SetCurrentPainLevel(Entity<PainComponent> ent, int newLevel)
    {
        if (ent.Comp.CurrentPainLevel == newLevel)
            return;

        var previousPainLevel = ent.Comp.CurrentPainLevel;
        ent.Comp.CurrentPainLevel = newLevel;
        DirtyField(ent, ent.Comp, nameof(PainComponent.CurrentPainLevel));

        var ev = new PainLevelChangedEvent(ent, previousPainLevel, ent.Comp.CurrentPainLevel);
        RaiseLocalEvent(ent, ref ev, true);
    }

    private FixedPoint2 GetDamageGroupPain(DamageSpecifier damage, ProtoId<DamageGroupPrototype> damageGroup, FixedPoint2 painMultiplier)
    {
        if (painMultiplier != 0 &&
            _prototypes.TryIndex(damageGroup, out var groupPrototype) &&
            damage.TryGetDamageInGroup(groupPrototype, out var groupDamage))
        {
            return groupDamage * painMultiplier;
        }
        return FixedPoint2.Zero;
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

            if (pain.CurrentPain == 0 &&
                pain.CurrentPainPercentage == 0 &&
                pain.CurrentPainLevel == 0 &&
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
            UpdateCurrentPainPercentage((uid, pain));

            if (time >= pain.NextPainLevelUpdateTime)
            {
                pain.NextPainLevelUpdateTime = time + pain.PainLevelUpdateRate;
                DirtyField(uid, pain, nameof(PainComponent.NextPainLevelUpdateTime));

                // Get the highest level in `PainLevels` whose threshold has been passed by `CurrentPainPercentage`.
                var newPainLevel = pain.PainLevels.FindLastIndex(level => level.Threshold <= pain.CurrentPainPercentage);
                StepCurrentPainLevel((uid, pain), newPainLevel);
            }

            // Server-side only from this point because `EntityEffect`s are seemingly unable to be serialized over to the client.
            if (_net.IsClient)
                continue;

            // Trigger any effects defined for this pain level.
            var currentEffectList = pain.PainLevels[pain.CurrentPainLevel].LevelEffects;
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
