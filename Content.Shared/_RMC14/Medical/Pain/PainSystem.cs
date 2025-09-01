using System.Linq;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;
using Content.Shared.EntityEffects;
using Robust.Shared.Timing;
using Robust.Shared.Random;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Alert;
using Content.Shared.Mobs.Systems;
using Content.Shared.Mobs.Events;
using Content.Shared.Rejuvenate;

namespace Content.Shared._RMC14.Medical.Pain;

public sealed partial class PainSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;

    private static readonly ProtoId<DamageGroupPrototype> BruteGroup = "Brute";
    private static readonly ProtoId<DamageGroupPrototype> BurnGroup = "Burn";
    private static readonly ProtoId<DamageGroupPrototype> ToxinGroup = "Toxin";
    private static readonly ProtoId<DamageGroupPrototype> AirlossGroup = "Airloss";

    private readonly HashSet<ProtoId<DamageTypePrototype>> _bruteTypes = new();
    private readonly HashSet<ProtoId<DamageTypePrototype>> _burnTypes = new();
    private readonly HashSet<ProtoId<DamageTypePrototype>> _toxinTypes = new();
    private readonly HashSet<ProtoId<DamageTypePrototype>> _airlossTypes = new();

    public override void Initialize()
    {
        SubscribeLocalEvent<PainComponent, DamageChangedEvent>(OnDamageChanged);
        SubscribeLocalEvent<PainComponent, BeforeAlertSeverityCheckEvent>(OnAlertSeverityCheck);
        SubscribeLocalEvent<PainComponent, RejuvenateEvent>(OnRejuvenate);

        _bruteTypes.Clear();
        _burnTypes.Clear();
        _toxinTypes.Clear();
        _airlossTypes.Clear();

        if (_prototypes.TryIndex(BruteGroup, out var bruteProto))
        {
            foreach (var type in bruteProto.DamageTypes)
            {
                _bruteTypes.Add(type);
            }
        }

        if (_prototypes.TryIndex(BurnGroup, out var burnProto))
        {
            foreach (var type in burnProto.DamageTypes)
            {
                _burnTypes.Add(type);
            }
        }

        if (_prototypes.TryIndex(ToxinGroup, out var toxinProto))
        {
            foreach (var type in toxinProto.DamageTypes)
            {
                _toxinTypes.Add(type);
            }
        }

        if (_prototypes.TryIndex(AirlossGroup, out var airlossProto))
        {
            foreach (var type in airlossProto.DamageTypes)
            {
                _airlossTypes.Add(type);
            }
        }
    }

    // TODO: fix movement speed effect
    private void OnRejuvenate(EntityUid uid, PainComponent pain, ref RejuvenateEvent args)
    {
        pain.PainModificators = [];
        pain.CurrentPain = 0;
        pain.CurrentPainPercentage = 0;
        pain.CurrentPainLevel = 0;
        _alerts.ShowAlert(uid, pain.Alert, 0);
    }

    private void OnAlertSeverityCheck(EntityUid uid, PainComponent pain, ref BeforeAlertSeverityCheckEvent args)
    {
        if (args.CurrentAlert == pain.Alert)
        {
            args.Severity = Math.Min((short)pain.CurrentPainLevel, _alerts.GetMaxSeverity(pain.Alert));
            args.CancelUpdate = true;
        }
    }

    private void OnDamageChanged(EntityUid uid, PainComponent comp, ref DamageChangedEvent args)
    {
        var damageDict = args.Damageable.Damage.DamageDict;
        UpdateCurrentPain(uid, comp, damageDict);
        UpdateCurrentPainPercentage(uid, comp);
    }

    public void TryChangePainLevelTo(EntityUid uid, int level, PainComponent? pain = null)
    {
        if (!Resolve(uid, ref pain))
            return;

        if (pain.NextPainLevelUpdateTime > _timing.CurTime)
            return;

        pain.NextPainLevelUpdateTime = _timing.CurTime + pain.PainLevelUpdateRate;
        pain.CurrentPainLevel += level.CompareTo(pain.CurrentPainLevel);

        DirtyField(uid, pain, nameof(PainComponent.CurrentPainLevel));
        DirtyField(uid, pain, nameof(PainComponent.NextPainLevelUpdateTime));

        if (pain.CurrentPainLevel <= _alerts.GetMaxSeverity(pain.Alert))
            _alerts.ShowAlert(uid, pain.Alert, (short)pain.CurrentPainLevel);
    }
    public void AddPainModificator(EntityUid uid, TimeSpan duration, FixedPoint2 effectStrength, PainModificatorType type, PainComponent? pain = null)
    {
        var expireAt = _timing.CurTime + duration;
        var mod = new PainModificator(expireAt, effectStrength, type);
        AddPainModificator(uid, mod, pain);
    }

    public void AddPainModificator(EntityUid uid, PainModificator mod, PainComponent? pain = null)
    {
        if (!Resolve(uid, ref pain))
            return;

        pain.PainModificators.Add(mod);
        UpdateCurrentPainPercentage(uid, pain);
        DirtyField(uid, pain, nameof(PainComponent.PainModificators));
    }

    private void UpdateCurrentPainPercentage(EntityUid uid, PainComponent comp)
    {
        var maxPainReductionModificatorStrength = FixedPoint2.Zero;
        var painIncrease = FixedPoint2.Zero;
        var painIncreases = comp.PainModificators.Where(mod => mod.Type == PainModificatorType.PainIncrease);
        var painReductions = comp.PainModificators.Where(mod => mod.Type == PainModificatorType.PainReduction);
        // get max pain reduction, sum pain increase
        if (painIncreases.Any())
            painIncrease = painIncreases.Select(mod => mod.EffectStrength).Sum();
        if (painReductions.Any())
            maxPainReductionModificatorStrength = painReductions.Max(mod => mod.EffectStrength);

        var realCurrentPain = comp.CurrentPain + painIncrease;
        // Pain reduction effectiveness linear decreases as the pain goes up
        var newPainReduction = FixedPoint2.Max(0, -realCurrentPain * comp.PainReductionDecreaceRate + maxPainReductionModificatorStrength);
        comp.CurrentPainPercentage = FixedPoint2.Clamp(realCurrentPain - newPainReduction, 0, 100);
        DirtyField(uid, comp, nameof(PainComponent.CurrentPainPercentage));
    }

    private void UpdateCurrentPain(EntityUid uid, PainComponent comp, Dictionary<string, FixedPoint2> damageDict)
    {
        var newCurrentPain = FixedPoint2.Zero;
        foreach (var (type, _) in damageDict)
        {
            if (_bruteTypes.Contains(type))
            {
                newCurrentPain += comp.BrutePainMultiplier * damageDict[type];
            }

            if (_burnTypes.Contains(type))
            {
                newCurrentPain += comp.BurnPainMultiplier * damageDict[type];
            }

            if (_toxinTypes.Contains(type))
            {
                newCurrentPain += comp.ToxinPainMultiplier * damageDict[type];
            }

            if (_airlossTypes.Contains(type))
            {
                newCurrentPain += comp.AirlossPainMultiplier * damageDict[type];
            }
        }

        comp.CurrentPain = newCurrentPain;

        DirtyField(uid, comp, nameof(PainComponent.CurrentPain));
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var time = _timing.CurTime;
        var painQuery = EntityQueryEnumerator<PainComponent>();
        while (painQuery.MoveNext(out var uid, out var pain))
        {
            if (time < pain.NextEffectUpdateTime)
                continue;

            if (_mobState.IsDead(uid))
                continue;

            pain.NextEffectUpdateTime = time + pain.EffectUpdateRate;
            DirtyField(uid, pain, nameof(PainComponent.NextEffectUpdateTime));

            pain.PainModificators.RemoveAll(mod => time > mod.ExpireAt);
            DirtyField(uid, pain, nameof(PainComponent.PainModificators));
            UpdateCurrentPainPercentage(uid, pain);

            var painLevels = pain.PainLevels.OrderBy(level => level.Threshold).ToList(); // in case someone writes it in the wrong order
            var isExpectedUpdated = false;
            var expectedPainLevel = 0;

            for (var i = 0; i < painLevels.Count; i++)
            {
                if (painLevels[i].Threshold < pain.CurrentPainPercentage)
                {
                    expectedPainLevel = i;  // update index to current element
                    isExpectedUpdated = true;
                }
                else
                {
                    break; // as list is sorted, no need to check further
                }
            }

            if (isExpectedUpdated)
            {
                TryChangePainLevelTo(uid, expectedPainLevel, pain);
                DirtyField(uid, pain, nameof(PainComponent.CurrentPainLevel));
            }

            if (painLevels.Count == 0 || !isExpectedUpdated)
                continue;

            var currentEffectList = painLevels[pain.CurrentPainLevel].LevelEffects;

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
