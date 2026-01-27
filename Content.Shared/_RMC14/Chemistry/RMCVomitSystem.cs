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
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Chemistry;

public sealed class RMCVomitSystem : EntitySystem
{
    [Dependency] private readonly INetManager _netManager = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedBloodstreamSystem _bloodstream = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly HungerSystem _hunger = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedPuddleSystem _puddle = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;

    private static readonly ProtoId<DamageGroupPrototype> ToxinGroup = "Toxin";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RMCVomitEvent>(OnRMCVomit);
        SubscribeLocalEvent<RMCDoVomitEvent>(OnRMCDoVomit);
    }

    /// <summary>
    /// Handles the vomit() proc from 13 - starts the delayed vomit sequence.
    /// </summary>
    private void OnRMCVomit(ref RMCVomitEvent args)
    {
        StartVomit(args.Target, args.HungerLoss, args.ToxinHeal);
    }

    /// <summary>
    /// Handles the do_vomit() proc from 13 - performs the actual vomit immediately.
    /// </summary>
    private void OnRMCDoVomit(ref RMCDoVomitEvent args)
    {
        DoVomit(args.Target, args.HungerLoss, args.ToxinHeal);
    }

    /// <summary>
    /// Start the vomit process.
    /// Shows nausea message, schedules warning and actual vomit.
    /// </summary>
    public void StartVomit(EntityUid uid, float hungerLoss = -40f, float toxinHeal = 3f)
    {
        if (_mobState.IsDead(uid))
            return;
        if (HasComp<SynthComponent>(uid))
            return;
        if (HasComp<RMCVomitComponent>(uid))
            return;

        // Add component to track vomit state and get timing values
        var vomitComp = EnsureComp<RMCVomitComponent>(uid);
        vomitComp.IsVomiting = true;

        _popup.PopupEntity(Loc.GetString("rmc-vomit-nausea"), uid, uid);

        // Warning message after 15 seconds
        Timer.Spawn(vomitComp.WarningDelay,
            () =>
        {
            if (!Exists(uid))
                return;
            if (!HasComp<RMCVomitComponent>(uid))
                return;

            _popup.PopupEntity(Loc.GetString("rmc-vomit-warning"), uid, uid);
        });

        // Actual vomit after 25 seconds
        Timer.Spawn(vomitComp.VomitDelay,
            () =>
        {
            if (!Exists(uid))
                return;
            if (!HasComp<RMCVomitComponent>(uid))
                return;

            DoVomit(uid, hungerLoss, toxinHeal);
        });
    }

    /// <summary>
    /// Make an entity vomit immediately.
    /// </summary>
    public void DoVomit(EntityUid uid, float hungerLoss = -40f, float toxinHeal = 3f)
    {
        if (_mobState.IsDead(uid))
            return;
        if (HasComp<SynthComponent>(uid))
            return;

        var vomitComp = EnsureComp<RMCVomitComponent>(uid);
        if (!vomitComp.IsVomiting)
            vomitComp.IsVomiting = true;

        if (vomitComp.VomitStunDuration > TimeSpan.Zero)
            _stun.TryStun(uid, vomitComp.VomitStunDuration, true);

        // Create vomit solution
        var solution = new Solution();
        var solutionSize = MathF.Abs(hungerLoss) / 3f;

        // Adds a tiny amount of the chem stream from earlier along with vomit -- Code from upstream VomitSystem
        if (TryComp<BloodstreamComponent>(uid, out var bloodStream))
        {
            var vomitAmount = solutionSize;

            // Flushes small portion of the chemicals removed from the bloodstream // TODO RMC14 (uid, bloodStream.BloodSolutionName, ref bloodStream.BloodSolution)
            if (_solutionContainer.ResolveSolution(uid, bloodStream.ChemicalSolutionName, ref bloodStream.ChemicalSolution))
            {
                var vomitChemstreamAmount = _solutionContainer.SplitSolution(bloodStream.ChemicalSolution.Value, vomitAmount);
                vomitChemstreamAmount.ScaleSolution(vomitComp.ChemMultiplier);
                solution.AddSolution(vomitChemstreamAmount, _proto);

                vomitAmount -= (float)vomitChemstreamAmount.Volume;
                /* // TODO RMC14 Replace above with this when BloodstreamSystem is updated
                var vomitChemstreamAmount = _bloodstream.FlushChemicals((uid, bloodStream), vomitAmount);

                if (vomitChemstreamAmount != null)
                {
                    vomitChemstreamAmount.ScaleSolution(ChemMultiplier);
                    solution.AddSolution(vomitChemstreamAmount, _proto);
                    vomitAmount -= (float)vomitChemstreamAmount.Volume;
                }
                */
            }

            // Makes a vomit solution the size of 90% of the chemicals removed from the chemstream // TODO RMC14 ((uid, bloodStream))), vomitAmount)
            solution.AddReagent(new ReagentId(vomitComp.VomitPrototype, _bloodstream.GetEntityBloodData(uid)), vomitAmount);
        }

        if (_puddle.TrySpillAt(uid, solution, out var puddle, false))
        {
            // TODO RMC14 SharedForensicsSystem Dependency Injection
            // _forensics.TransferDna(puddle, uid, false);
            var ev = new TransferDnaEvent { Donor = uid, Recipient = puddle, CanDnaBeCleaned = false };
            RaiseLocalEvent(uid, ref ev);
        }

        if (TryComp<HungerComponent>(uid, out var hunger))
            _hunger.ModifyHunger(uid, hungerLoss, hunger);

        if (toxinHeal > 0)
        {
            var rmcDamageable = EntityManager.System<SharedRMCDamageableSystem>();
            var healing = rmcDamageable.DistributeHealingCached(uid, ToxinGroup, toxinHeal);
            _damageable.TryChangeDamage(uid, healing, true, interruptsDoAfters: false);
        }

        _popup.PopupEntity(Loc.GetString("rmc-vomit-others", ("person", Identity.Entity(uid, EntityManager))), uid);
        _popup.PopupEntity(Loc.GetString("rmc-vomit-self"), uid, uid);

        if (_netManager.IsServer)
            _audio.PlayPvs(vomitComp.VomitSound, uid);

        // Reset cooldown 35 seconds after vomit
        Timer.Spawn(vomitComp.CooldownAfterVomit,
            () =>
        {
            if (!Exists(uid))
                return;
            RemComp<RMCVomitComponent>(uid);
        });
    }
}
