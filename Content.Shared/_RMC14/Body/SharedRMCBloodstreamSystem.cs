using Content.Shared._RMC14.BlurredVision;
using Content.Shared._RMC14.Chemistry.Reagent;
using Content.Shared._RMC14.Stun;
using Content.Shared._RMC14.Synth;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs.Systems;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Rejuvenate;
using Content.Shared.StatusEffect;
using Content.Shared.Stunnable;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using System.Diagnostics.CodeAnalysis;

namespace Content.Shared._RMC14.Body;

public abstract class SharedRMCBloodstreamSystem : EntitySystem
{
    [Dependency] private readonly RMCReagentSystem _rmcReagent = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;
    [Dependency] private readonly SharedBloodstreamSystem _bloodstream = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly MobStateSystem _mob = default!;
    [Dependency] private readonly HungerSystem _hunger = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly RMCSizeStunSystem _rmcStun = default!;
    [Dependency] private readonly StatusEffectsSystem _status = default!;
    [Dependency] private readonly MobThresholdSystem _mobThreshold = default!;

    private static readonly TimeSpan MaxEyeBlurTime = TimeSpan.FromSeconds(5);
    private readonly List<ReagentId> _reagentsToRemove = new();

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCBloodComponent, MapInitEvent>(OnRMCBloodMapInit, after: [typeof(SharedBloodstreamSystem)]);
        SubscribeLocalEvent<RMCBloodComponent, RejuvenateEvent>(OnRMCBloodRejuvenate, after: [typeof(SharedBloodstreamSystem)]);
    }

    public virtual bool TryGetBloodSolution(EntityUid uid, [NotNullWhen(true)] out Solution? solution)
    {
        // TODO RMC14
        return _solution.TryGetSolution(uid, "bloodstream", out _, out solution);
    }

    public virtual bool TryGetChemicalSolution(EntityUid uid, out Entity<SolutionComponent> solutionEnt, [NotNullWhen(true)] out Solution? solution)
    {
        // TODO RMC14
        solutionEnt = default;
        if (!_solution.TryGetSolution(uid, "chemicals", out var nullableSolutionEnt, out solution))
            return false;

        solutionEnt = nullableSolutionEnt.Value;
        return true;
    }

    public virtual bool IsBleeding(EntityUid uid)
    {
        // TODO RMC14
        return false;
    }

    public void RemoveBloodstreamToxins(EntityUid body, FixedPoint2 amount)
    {
        if (!TryGetChemicalSolution(body, out var solutionEnt, out _))
            return;

        _reagentsToRemove.Clear();
        foreach (var content in solutionEnt.Comp.Solution.Contents)
        {
            if (!_rmcReagent.TryIndex(content.Reagent, out var reagent))
                continue;

            if (!reagent.Toxin)
                continue;

            _reagentsToRemove.Add(content.Reagent);
        }

        foreach (var remove in _reagentsToRemove)
        {
            _solution.RemoveReagent(solutionEnt, remove, amount);
        }
    }

    public void RemoveBloodstreamChemical(EntityUid body, ProtoId<ReagentPrototype> reagentId, FixedPoint2 amount)
    {
        if (!TryGetChemicalSolution(body, out var solutionEnt, out _))
            return;

        _solution.RemoveReagent(solutionEnt, reagentId, amount);
    }

    public bool RemoveBloodstreamAlcohols(EntityUid body, FixedPoint2 amount)
    {
        if (!TryGetChemicalSolution(body, out var solutionEnt, out _))
            return false;

        _reagentsToRemove.Clear();
        foreach (var content in solutionEnt.Comp.Solution.Contents)
        {
            if (!_rmcReagent.TryIndex(content.Reagent, out var reagent))
                continue;

            if (!reagent.Alcohol)
                continue;

            _reagentsToRemove.Add(content.Reagent);
        }

        var alcoholRemoved = _reagentsToRemove.Count > 0;

        foreach (var remove in _reagentsToRemove)
        {
            _solution.RemoveReagent(solutionEnt, remove, amount);
        }

        return alcoholRemoved;
    }

    private void OnRMCBloodMapInit(Entity<RMCBloodComponent> ent, ref MapInitEvent args)
    {
        var time = _timing.CurTime;
        ent.Comp.NextBleedingUpdate = time + ent.Comp.DoBleedEvery;
        ent.Comp.NextBloodUpdate = time + ent.Comp.UpdateBloodEvery;
        if (TryGetBloodSolution(ent, out var solution) && TryComp<BloodstreamComponent>(ent, out var bloodstream))
        {
            if (solution.Volume > ent.Comp.BloodNormal)
                solution.RemoveSolution(solution.Volume - ent.Comp.BloodNormal);
            else if (solution.Volume < ent.Comp.BloodNormal)
                solution.AddReagent(bloodstream.BloodReagent, ent.Comp.BloodNormal - solution.Volume);
        }
        Dirty(ent);
    }

    private void OnRMCBloodRejuvenate(Entity<RMCBloodComponent> ent, ref RejuvenateEvent args)
    {
        if (TryGetBloodSolution(ent, out var solution) && TryComp<BloodstreamComponent>(ent, out var bloodstream))
        {
            if (solution.Volume > ent.Comp.BloodNormal)
                solution.RemoveSolution(solution.Volume - ent.Comp.BloodNormal);
            else if (solution.Volume < ent.Comp.BloodNormal)
                solution.AddReagent(bloodstream.BloodReagent, ent.Comp.BloodNormal - solution.Volume);
        }
        Dirty(ent);
    }

    public bool TryGetBloodVolumes(EntityUid ent, out FixedPoint2 curBlood, out FixedPoint2 normalBlood)
    {
        curBlood = 0;
        normalBlood = 0;

        if (!TryGetBloodSolution(ent, out var solu))
            return false;

        curBlood = solu.Volume;

        if (TryComp<RMCBloodComponent>(ent, out var rmcblood))
            normalBlood = rmcblood.BloodNormal;
        else
            normalBlood = solu.MaxVolume;

        return true;
    }

    public void UpdateBlood(Entity<BloodstreamComponent, RMCBloodComponent> ent, TimeSpan curTime)
    {
        if (curTime >= ent.Comp2.NextBleedingUpdate)
        {
            ent.Comp2.NextBleedingUpdate += ent.Comp2.DoBleedEvery;
            DoBleeding(ent);
        }

        if (curTime >= ent.Comp2.NextBloodUpdate)
        {
            ent.Comp2.NextBloodUpdate += ent.Comp2.UpdateBloodEvery;
            ProcessBlood(ent, curTime);
        }
    }

    private void DoBleeding(Entity<BloodstreamComponent, RMCBloodComponent> ent)
    {
        if (ent.Comp1.BleedAmount > 0)
        {
            // Blood is removed from the bloodstream at a 1-1 rate with the bleed amount
            // There is no passive reduction here - bleed stop is handled by the wound system
            _bloodstream.TryModifyBloodLevel((ent, ent.Comp1), -ent.Comp1.BleedAmount);
        }
    }

    private void ProcessBlood(Entity<BloodstreamComponent, RMCBloodComponent> ent, TimeSpan curTime)
    {
        if (!ent.Comp2.ProcessBlood)
            return;

        if (_mob.IsDead(ent) ||
            !TryGetBloodSolution(ent, out var blood) ||
            !TryComp<DamageableComponent>(ent, out var damage)) //TODO RMC14 do not process when in cryo
            return;

        var bloodVolume = blood.Volume;

        bool isSynth = HasComp<SynthComponent>(ent);

        //Blood regen

        if (bloodVolume < ent.Comp2.BloodNormal &&
            TryComp<HungerComponent>(ent, out var hunger) &&
            _hunger.GetHunger(hunger) >= ent.Comp2.BloodRegenNutritionCost)
        {
            _bloodstream.TryModifyBloodLevel((ent, ent.Comp1), ent.Comp2.BloodRegen);
            _hunger.ModifyHunger(ent, -ent.Comp2.BloodRegenNutritionCost);
        }
        else if (bloodVolume > ent.Comp2.BloodNormal)
            blood.RemoveSolution(ent.Comp2.OverlimitBloodLoss);

        //TODO RMC14 reduce max blood depending on heart health

        if (bloodVolume > ent.Comp2.BloodSafe || !_proto.TryIndex(ent.Comp2.OxygenDamage, out var oxyDamage))
            return;

        //Bloodloss effects
        var bloodPercent = bloodVolume / (ent.Comp2.BloodNormal / 100);

        //variable oxyloss - later effects may provide their own oxyloss
        var vOxyloss = (100 - bloodPercent) / ent.Comp2.VOxylossDivisor;

        var curOxyloss = damage.Damage.TryGetDamageInGroup(oxyDamage, out var oxy) ? oxy : 0;

        var maxVOxyloss = FixedPoint2.Clamp((100 - bloodPercent) / 2, curOxyloss, ent.Comp2.MaxVOxyloss);

        if (curOxyloss < maxVOxyloss)
        {
            //vOxyloss is floored before being afflicted
            var vOxyDamage = new DamageSpecifier(oxyDamage, FixedPoint2.Max((int)vOxyloss, 0));
            _damage.TryChangeDamage(ent, vOxyDamage, true, interruptsDoAfters: false);
        }

        if (_net.IsClient) //TODO RMC14 remove when randomizer is synced with server & client like gun prediction
            return;

        if (bloodVolume >= ent.Comp2.BloodOkay)
        {
            if (_random.Prob(0.01f))
                if (isSynth)
                    _popup.PopupEntity(Loc.GetString("rmc-blood-okay-synth"), ent, ent, PopupType.SmallCaution);
                else
                    PopupLowBloodMessage(ent, null);
        }
        else if (bloodVolume >= ent.Comp2.BloodBad)
        {
            if (isSynth)
            {
                if (_random.Prob(0.03f))
                {
                    _stun.TryParalyze(ent, TimeSpan.FromSeconds(_random.Next(2, 5)), true);
                    _popup.PopupEntity(Loc.GetString("rmc-blood-bad-synth"), ent, ent, PopupType.SmallCaution);
                }
            }
            else
            {
                if (TryAddBlur(ent, curTime))
                {
                    var badBloodDamage = new DamageSpecifier(oxyDamage, 3);
                    _damage.TryChangeDamage(ent, badBloodDamage, true, interruptsDoAfters: false);
                }

                if (_random.Prob(0.15f))
                {
                    _rmcStun.TryKnockOut(ent, TimeSpan.FromSeconds(_random.Next(2, 7)), true);
                    PopupLowBloodMessage(ent, "rmc-blood-very");
                }
            }
        }
        else if (bloodVolume >= ent.Comp2.BloodSurvive)
        {
            if (isSynth)
            {
                if (_random.Prob(0.05f))
                {
                    _stun.TryParalyze(ent, TimeSpan.FromSeconds(_random.Next(2, 5)), true);
                    _popup.PopupEntity(Loc.GetString("rmc-blood-survive-synth"), ent, ent, PopupType.SmallCaution);
                }
            }
            else
            {
                if (TryAddBlur(ent, curTime))
                {
                    if (_proto.TryIndex(ent.Comp2.ToxinDamage, out var toxDamage))
                    {
                        var surviveBloodDamage = new DamageSpecifier(oxyDamage, 8) + new DamageSpecifier(toxDamage, 3);
                        _damage.TryChangeDamage(ent, surviveBloodDamage, true, interruptsDoAfters: false);
                    }
                }

                if (_random.Prob(0.15f))
                {
                    _rmcStun.TryKnockOut(ent, TimeSpan.FromSeconds(_random.Next(2, 7)), true);
                    PopupLowBloodMessage(ent, "rmc-blood-extremely");
                }
            }
        }
        else
        {
            //Instant death
            if (!_mobThreshold.TryGetDeadThreshold(ent, out var deathThreshold) || deathThreshold == null)
                return;

            var killDamage = new DamageSpecifier(oxyDamage, deathThreshold.Value - damage.TotalDamage);
            _damage.TryChangeDamage(ent, killDamage, true, interruptsDoAfters: false);
        }
    }

    private void PopupLowBloodMessage(EntityUid ent, LocId? severity)
    {
        string condition = _random.Pick(new List<string> { "rmc-blood-dizzy", "rmc-blood-woozy", "rmc-blood-faint" });
        string message = string.Empty;

        if (severity == null)
            message = Loc.GetString("rmc-blood-message-simple", ("condition", Loc.GetString(condition)));
        else
            message = Loc.GetString("rmc-blood-message", ("severity", Loc.GetString(severity)), ("condition", Loc.GetString(condition)));

        _popup.PopupEntity(message, ent, ent, PopupType.SmallCaution);
    }

    private bool TryAddBlur(EntityUid ent, TimeSpan curTime)
    {
        if (_status.TryGetTime(ent, "Blinded", out var blur) &&
            blur.Value.Item2 - curTime >= MaxEyeBlurTime)
            return false;

        _status.TryAddStatusEffect<RMCBlindedComponent>(ent, "Blinded", TimeSpan.FromSeconds(12), false);
        return true;
    }
}
