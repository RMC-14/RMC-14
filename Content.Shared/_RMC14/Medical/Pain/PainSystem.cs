using Content.Shared._RMC14.Damage;
using Content.Shared.Alert;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs;
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

    public override void Initialize()
    {
        SubscribeLocalEvent<PainComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<PainComponent, AfterAutoHandleStateEvent>(OnPainState);
        SubscribeLocalEvent<PainComponent, DamageChangedEvent>(OnDamageChanged);
        SubscribeLocalEvent<PainComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<PainComponent, BeforeAlertSeverityCheckEvent>(OnAlertSeverityCheck);
        SubscribeLocalEvent<PainComponent, RejuvenateEvent>(OnRejuvenate);
    }

    /// <summary>
    /// Add a new <see cref="PainModifier"/> to <paramref name="ent"/>'s <see cref="PainComponent.PainModifiers"/>,
    /// to be removed when its <see cref="PainModifier.ExpireAt"/> time is reached.
    /// </summary>
    public void AddPainModifier(Entity<PainComponent?> ent, PainModifier mod)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        ent.Comp.PainModifiers.Add(mod);
        DirtyField(ent, ent.Comp, nameof(PainComponent.PainModifiers));
    }

    /// <inheritdoc cref="AddPainModifier(Entity{PainComponent?}, PainModifier)"/>
    public void AddPainModifier(Entity<PainComponent?> ent, TimeSpan duration, FixedPoint2 effectStrength, PainModifierType type)
    {
        var expireAt = _timing.CurTime + duration;
        var mod = new PainModifier(expireAt, effectStrength, type);
        AddPainModifier(ent, mod);
    }

    /// <summary>
    /// Remove all currently active <see cref="PainModifier"/>s in <paramref name="ent"/>'s <see cref="PainComponent.PainModifiers"/>,
    /// regardless of whether they've reached their <see cref="PainModifier.ExpireAt"/> time or not.
    /// </summary>
    /// <remarks>
    /// Modifiers from painkillers or other <see cref="EntityEffect"/>s will automatically re-apply themselves the next tick.
    /// In order to prevent that, the source reagent/effect needs to be removed as well.
    /// </remarks>
    public void ClearPainModifiers(Entity<PainComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        ent.Comp.PainModifiers.Clear();
        DirtyField(ent, ent.Comp, nameof(PainComponent.PainModifiers));
    }

    /// <summary>
    /// Find and return the highest <see cref="PainLevel"/> in <paramref name="painComp"/>'s <see cref="PainComponent.PainLevels"/> list
    /// where <c>PainLevel.Threshold &lt;= {painValue}</c>.<br/>
    /// <c>{painValue}</c> is either <paramref name="painValueOverride"/> if provided, or <paramref name="painComp"/>'s <see cref="PainComponent.PerceivedPain"/> if not.
    /// </summary>
    /// <param name="painComp">The <see cref="PainComponent"/> whose <see cref="PainComponent.PainLevels"/> list is being checked.</param>
    /// <param name="painValueOverride">(Optional) Pain value to compare against each <see cref="PainLevel.Threshold"/> instead of using <see cref="PainComponent.PerceivedPain"/>.</param>
    /// <returns>
    /// Tuple of the matching <see cref="PainLevel"/> object and its index in <see cref="PainComponent.PainLevels"/>.
    /// </returns>
    public (PainLevel Level, int Index) GetHighestPainLevelReached(PainComponent painComp, FixedPoint2? painValueOverride = null)
    {
        if (painValueOverride < FixedPoint2.Zero)
            throw new ArgumentOutOfRangeException(nameof(painValueOverride), painValueOverride, "Pain values shouldn't ever go below zero!");

        var painValue = painValueOverride ?? painComp.PerceivedPain;
        for (var i = painComp.PainLevels.Count - 1; i >= 0; i--)
        {
            var painLevel = painComp.PainLevels[i];
            if (painLevel.Threshold <= painValue)
                return (painLevel, i);
        }

        // should have been caught by the assertion in `OnInit()` below, but just it wasn't (and to appease the compiler)
        throw new InvalidOperationException($"The first pain level in {nameof(PainComponent)}.{nameof(PainComponent.PainLevels)} must have a `Threshold` value of 0.");
    }

    private void OnInit(Entity<PainComponent> ent, ref ComponentInit args)
    {
        DebugTools.AssertEqual(ent.Comp.PainLevels.First().Threshold, 0,
            $"Assert failed for {ToPrettyString(ent)}: The first pain level in {nameof(PainComponent)}.{nameof(PainComponent.PainLevels)} must have a `Threshold` value of 0.");
        DebugTools.Assert(ent.Comp.PainLevels.SequenceEqual(ent.Comp.PainLevels.OrderBy(level => level.Threshold)),
            $"Assert failed for {ToPrettyString(ent)}: {nameof(PainComponent)}.{nameof(PainComponent.PainLevels)} entries must be written in order of their `Threshold`s. (Low -> High)");
    }

    /// <summary>
    /// Used to force an update of the client-side damage overlay if the server overrides the client's component state with different values,<br/>
    /// specifically the <see cref="PainComponent.PerceivedPain"/> and <see cref="PainComponent.CurrentPainLevelIdx"/> datafields.
    /// </summary>
    /// <remarks>
    /// The overlay is <i>usually</i> updated either by its own event subscriptions (e.g. <see cref="MobThresholdChecked"/>), or by the setter methods seen below.<br/>
    /// If the server overrides the client's component state though, the client never gets a chance to use the setters.<br/>
    /// This is here to catch that case and raise <see cref="DamageOverlayUpdateEvent"/> manually.
    /// </remarks>
    /// <seealso cref="PainComponent.PreviousPerceivedPain"/>
    /// <seealso cref="PainComponent.PreviousPainLevelIdx"/>
    /// <seealso cref="SetPerceivedPain(Entity{PainComponent}, FixedPoint2)"/>
    /// <seealso cref="SetCurrentPainLevelIdx(Entity{PainComponent}, int)"/>
    private void OnPainState(Entity<PainComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (ent.Comp.PreviousPerceivedPain != ent.Comp.PerceivedPain)
        {
            ent.Comp.PreviousPerceivedPain = ent.Comp.PerceivedPain;
            var ev = new DamageOverlayUpdateEvent(ent);
            RaiseLocalEvent(ent, ref ev, true);
        }
        if (ent.Comp.PreviousPainLevelIdx != ent.Comp.CurrentPainLevelIdx)
        {
            ent.Comp.PreviousPainLevelIdx = ent.Comp.CurrentPainLevelIdx;
            var ev = new DamageOverlayUpdateEvent(ent);
            RaiseLocalEvent(ent, ref ev, true);
        }
    }

    private void OnRejuvenate(Entity<PainComponent> ent, ref RejuvenateEvent args)
    {
        SetPerceivedPain(ent, 0);
        SetCurrentPainLevelIdx(ent, 0);
        ent.Comp.PainModifiers.Clear();
        DirtyField(ent, ent.Comp, nameof(PainComponent.PainModifiers));
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

        foreach (var (groupId, value) in ent.Comp.DamageGroupPainMultipliers)
            newPainValue += GetDamageGroupPain(damage, groupId, value);

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

    private void OnMobStateChanged(Entity<PainComponent> ent, ref MobStateChangedEvent args)
    {
        // Going from *not* dead to dead.
        if (args.NewMobState == MobState.Dead)
        {
            // Clear out all of their (relevant) `PainComponent` vars, just for the sake of preventing weird edge case behaviour while they're dead.
            // (shouldn't be feeling pain anyway if you're dead)
            // If the user gets revived then they all get set back to normal below.
            SetPerceivedPain(ent, 0);
            SetCurrentPainLevelIdx(ent, 0);
            ent.Comp.PainModifiers.Clear();
            DirtyField(ent, ent.Comp, nameof(PainComponent.PainModifiers));
        }
        // Going from dead to *not* dead.
        else if (args.OldMobState == MobState.Dead)
        {
            // Jump the vars back over to where they would have been if the system hadn't stopped updating after they died.
            // This *does* happen automatically in `Update()`, but that only moves `CurrentPainLevelIdx` one step at a time.
            // Setting it here is just to skip the wait time.
            UpdatePerceivedPain(ent);
            SetCurrentPainLevelIdx(ent, GetHighestPainLevelReached(ent).Index);
        }
    }

    /// <summary>
    /// Calculate a new value for <paramref name="ent"/>'s <see cref="PainComponent.PerceivedPain"/>, starting with their
    /// <see cref="PainComponent.BasePain"/> and adding any <see cref="PainModifier"/>s in the <see cref="PainComponent.PainModifiers"/> list.
    /// </summary>
    /// <remarks>
    /// Not to be confused with <see cref="SetPerceivedPain(Entity{PainComponent}, FixedPoint2)"/>, which is just a setter for the datafield.
    /// </remarks>
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

        SetPerceivedPain(ent, newPainPercentage);
    }

    /// <summary>
    /// Setter for <paramref name="ent"/>'s <see cref="PainComponent.PerceivedPain"/> datafield.
    /// </summary>
    /// <remarks>
    /// Not to be confused with <see cref="UpdatePerceivedPain(Entity{PainComponent})"/>.
    /// </remarks>
    private void SetPerceivedPain(Entity<PainComponent> ent, FixedPoint2 newValue)
    {
        if (newValue == ent.Comp.PerceivedPain)
            return;

        ent.Comp.PerceivedPain = newValue;
        DirtyField(ent, ent.Comp, nameof(PainComponent.PerceivedPain));

        var ev = new DamageOverlayUpdateEvent(ent);
        RaiseLocalEvent(ent, ref ev, true);
    }

    /// <summary>
    /// Setter for <paramref name="ent"/>'s <see cref="PainComponent.CurrentPainLevelIdx"/> datafield.
    /// </summary>
    private void SetCurrentPainLevelIdx(Entity<PainComponent> ent, int newValue)
    {
        if (newValue == ent.Comp.CurrentPainLevelIdx)
            return;

        ent.Comp.CurrentPainLevelIdx = newValue;
        DirtyField(ent, ent.Comp, nameof(PainComponent.CurrentPainLevelIdx));

        var ev = new DamageOverlayUpdateEvent(ent);
        RaiseLocalEvent(ent, ref ev, true);

        if (ent.Comp.CurrentPainLevelIdx <= _alerts.GetMaxSeverity(ent.Comp.Alert))
            _alerts.ShowAlert(ent, ent.Comp.Alert, (short)ent.Comp.CurrentPainLevelIdx);
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
            var uidEntity = new Entity<PainComponent>(uid, pain);
            UpdatePerceivedPain(uidEntity);

            if (time >= pain.NextPainLevelUpdateTime)
            {
                pain.NextPainLevelUpdateTime = time + pain.PainLevelUpdateRate;
                DirtyField(uid, pain, nameof(PainComponent.NextPainLevelUpdateTime));

                // Get the highest level in `PainLevels` whose threshold has been passed by `PerceivedPain`.
                var highestPainLevelIdx = GetHighestPainLevelReached(uidEntity).Index;

                // Move `currentPainLevelIdx` towards `highestPainLevelIdx` by one step.
                if (highestPainLevelIdx > pain.CurrentPainLevelIdx)
                    SetCurrentPainLevelIdx(uidEntity, pain.CurrentPainLevelIdx + 1);
                else if (highestPainLevelIdx < pain.CurrentPainLevelIdx)
                    SetCurrentPainLevelIdx(uidEntity, pain.CurrentPainLevelIdx - 1);
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
