using Content.Shared._RMC14.Damage;
using Content.Shared._RMC14.Synth;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Fluids;
using Content.Shared.Forensics;
using Content.Shared.IdentityManagement;
using Content.Shared.Mobs.Systems;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Stunnable;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Chemistry;

public sealed class RMCVomitSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedBloodstreamSystem _bloodstream = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    //[Dependency] private readonly SharedForensicsSystem _forensics = default!;
    [Dependency] private readonly HungerSystem _hunger = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SharedPuddleSystem _puddle = default!;
    [Dependency] private readonly SharedRMCDamageableSystem _rmcDamageable = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private static readonly ProtoId<DamageGroupPrototype> ToxinGroup = "Toxin";

    /// <summary>
    /// Start the delayed vomit process. Shows nausea message and schedules
    /// warning → vomit → cooldown via the <see cref="Update"/> loop.
    /// </summary>
    public void StartVomit(EntityUid uid, float hungerLoss = -40f, float toxinHeal = 3f)
    {
        if (_mobState.IsDead(uid))
            return;

        if (HasComp<SynthComponent>(uid))
            return;

        if (HasComp<RMCVomitComponent>(uid))
            return;

        var vomitComp = EnsureComp<RMCVomitComponent>(uid);
        vomitComp.Phase = RMCVomitPhase.Nausea;
        vomitComp.NextPhaseAt = _timing.CurTime + vomitComp.WarningDelay;
        vomitComp.HungerLoss = hungerLoss;
        vomitComp.ToxinHeal = toxinHeal;
        Dirty(uid, vomitComp);

        if (_net.IsServer)
            _popup.PopupEntity(Loc.GetString("rmc-vomit-nausea"), uid, uid, PopupType.MediumCaution);
    }

    /// <summary>
    /// Make an entity vomit immediately and enter cooldown.
    /// If the entity is already in a vomit sequence (nausea/warning), this is a no-op.
    /// </summary>
    public void DoVomit(EntityUid uid, float hungerLoss = -40f, float toxinHeal = 3f)
    {
        if (_mobState.IsDead(uid))
            return;

        if (HasComp<SynthComponent>(uid))
            return;

        // Don't interrupt vomit that is in-progress
        if (TryComp<RMCVomitComponent>(uid, out var existing) && existing.Phase != RMCVomitPhase.Cooldown)
            return;

        var vomitComp = EnsureComp<RMCVomitComponent>(uid);
        vomitComp.HungerLoss = hungerLoss;
        vomitComp.ToxinHeal = toxinHeal;

        PerformVomit(uid, vomitComp);

        vomitComp.Phase = RMCVomitPhase.Cooldown;
        vomitComp.NextPhaseAt = _timing.CurTime + vomitComp.CooldownAfterVomit;
        Dirty(uid, vomitComp);
    }

    /// <summary>
    /// Performs the actual vomit effects: stun, puddle, hunger loss, toxin healing, popups, sound.
    /// </summary>
    private void PerformVomit(EntityUid uid, RMCVomitComponent vomitComp)
    {
        if (vomitComp.VomitStunDuration > TimeSpan.Zero)
            _stun.TryStun(uid, vomitComp.VomitStunDuration, true);

        // Create vomit solution
        var solution = new Solution();
        var solutionSize = MathF.Abs(vomitComp.HungerLoss) / 3f;

        // Adds a tiny amount of the chem stream from earlier along with vomit — Code from upstream VomitSystem
        if (TryComp<BloodstreamComponent>(uid, out var bloodStream))
        {
            var vomitAmount = solutionSize;

            if (_solutionContainer.ResolveSolution(uid, bloodStream.ChemicalSolutionName, ref bloodStream.ChemicalSolution))
            {
                var vomitChemstreamAmount = _solutionContainer.SplitSolution(bloodStream.ChemicalSolution.Value, vomitAmount);
                vomitChemstreamAmount.ScaleSolution(vomitComp.ChemMultiplier);
                solution.AddSolution(vomitChemstreamAmount, _proto);

                vomitAmount -= (float) vomitChemstreamAmount.Volume;
            }

            solution.AddReagent(new ReagentId(vomitComp.VomitPrototype, _bloodstream.GetEntityBloodData(uid)), vomitAmount);
        }

        if (_puddle.TrySpillAt(uid, solution, out var puddle, false))
        {
            // TODO RMC14 use SharedForensicsSystem. TransferDnaEvent will eventually be deleted upstream.
            // _forensics.TransferDna(puddle, uid, false);
            var ev = new TransferDnaEvent { Donor = uid, Recipient = puddle, CanDnaBeCleaned = false };
            RaiseLocalEvent(uid, ref ev);
        }

        if (TryComp<HungerComponent>(uid, out var hunger))
            _hunger.ModifyHunger(uid, vomitComp.HungerLoss, hunger);

        if (vomitComp.ToxinHeal > 0)
        {
            var healing = _rmcDamageable.DistributeHealingCached(uid, ToxinGroup, vomitComp.ToxinHeal);
            _damageable.TryChangeDamage(uid, healing, true, interruptsDoAfters: false);
        }

        if (_net.IsClient)
            return;

        var othersPopup = Loc.GetString("rmc-vomit-others", ("person", Identity.Entity(uid, EntityManager)));
        _popup.PopupEntity(othersPopup, uid, Filter.PvsExcept(uid), true, PopupType.MediumCaution);
        _popup.PopupEntity(Loc.GetString("rmc-vomit-self"), uid, uid, PopupType.MediumCaution);
        _audio.PlayPvs(vomitComp.VomitSound, uid);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_net.IsClient)
            return;

        var curTime = _timing.CurTime;
        var query = EntityQueryEnumerator<RMCVomitComponent>();

        while (query.MoveNext(out var uid, out var comp))
        {
            if (_mobState.IsDead(uid))
            {
                RemCompDeferred<RMCVomitComponent>(uid);
                continue;
            }

            if (curTime < comp.NextPhaseAt)
                continue;

            switch (comp.Phase)
            {
                case RMCVomitPhase.Nausea:
                    _popup.PopupEntity(Loc.GetString("rmc-vomit-warning"), uid, uid, PopupType.MediumCaution);
                    comp.Phase = RMCVomitPhase.Warning;
                    comp.NextPhaseAt = curTime + (comp.VomitDelay - comp.WarningDelay);
                    Dirty(uid, comp);
                    break;

                case RMCVomitPhase.Warning:
                    PerformVomit(uid, comp);
                    comp.Phase = RMCVomitPhase.Cooldown;
                    comp.NextPhaseAt = curTime + comp.CooldownAfterVomit;
                    Dirty(uid, comp);
                    break;

                case RMCVomitPhase.Cooldown:
                    RemCompDeferred<RMCVomitComponent>(uid);
                    break;
            }
        }
    }
}
