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
    [Dependency] private readonly SharedBodySystem _body = default!;
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
        StartVomit(args.Target);
    }

    /// <summary>
    /// Handles the do_vomit() proc from 13 - performs the actual vomit immediately.
    /// </summary>
    private void OnRMCDoVomit(ref RMCDoVomitEvent args)
    {
        DoVomit(args.Target, args.StunDuration, args.HungerLoss, args.ToxinHeal);
    }

    /// <summary>
    /// Start the vomit process.
    /// Shows nausea message, schedules warning and actual vomit.
    /// </summary>
    public void StartVomit(EntityUid uid)
    {
        // Synthetics don't throw up
        if (HasComp<SynthComponent>(uid))
            return;

        if (_mobState.IsDead(uid))
            return;

        // Check if already vomiting (lastpuke check)
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
            if (!TryComp<RMCVomitComponent>(uid, out var comp))
                return;

            // Perform the actual vomit
            DoVomit(uid, comp.VomitStunDuration, comp.HungerLoss, comp.ToxinHeal);

            // Reset cooldown 35 seconds after DoVomit
            Timer.Spawn(comp.CooldownAfterVomit,
                () =>
            {
                if (!Exists(uid))
                    return;
                RemComp<RMCVomitComponent>(uid);
            });
        });
    }

    /// <summary>
    /// Make an entity vomit immediately.
    /// </summary>
    public void DoVomit(EntityUid uid, TimeSpan stunDuration, float hungerLoss = -40f, float toxinHeal = 3f)
    {
        if (_mobState.IsDead(uid))
            return;

        if (stunDuration > TimeSpan.Zero)
            _stun.TryStun(uid, stunDuration, true);

        // Get or ensure component for accessing configuration values
        var vomitComp = EnsureComp<RMCVomitComponent>(uid);

        // Create vomit solution
        var solution = new Solution();
        var vomitAmount = MathF.Abs(hungerLoss) / 4f;

        // Empty the stomach contents into the vomit - code from upstream VomitSystem
        var stomachList = _body.GetBodyOrganEntityComps<StomachComponent>(uid);
        foreach (var stomach in stomachList)
        {
            if (_solutionContainer.ResolveSolution(stomach.Owner, StomachSystem.DefaultSolutionName, ref stomach.Comp1.Solution, out var stomachSol))
            {
                solution.AddSolution(stomachSol, _proto);
                stomachSol.RemoveAllSolution();
                _solutionContainer.UpdateChemicals(stomach.Comp1.Solution.Value);
            }
        }

        // Add 10% of chemicals from bloodstream to the vomit - code from upstream VomitSystem
        if (TryComp<BloodstreamComponent>(uid, out var bloodStream))
        {
            if (_solutionContainer.ResolveSolution(uid, bloodStream.ChemicalSolutionName, ref bloodStream.ChemicalSolution))
            {
                var vomitChemstreamAmount = _solutionContainer.SplitSolution(bloodStream.ChemicalSolution.Value, vomitAmount);
                vomitChemstreamAmount.ScaleSolution(vomitComp.ChemMultiplier);
                solution.AddSolution(vomitChemstreamAmount, _proto);

                vomitAmount -= (float) vomitChemstreamAmount.Volume;
            }

            solution.AddReagent(new ReagentId(vomitComp.VomitPrototype, _bloodstream.GetEntityBloodData(uid)), vomitAmount);
        }
        else
        {
            solution.AddReagent(new ReagentId(vomitComp.VomitPrototype, null), vomitAmount);
        }

        if (_puddle.TrySpillAt(uid, solution, out var puddle, false))
        {
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

        if (!_netManager.IsServer)
            return;

        _audio.PlayPvs(vomitComp.VomitSound, uid);
        _popup.PopupEntity(Loc.GetString("rmc-vomit-others", ("person", Identity.Entity(uid, EntityManager))), uid);
        _popup.PopupEntity(Loc.GetString("rmc-vomit-self"), uid, uid);
    }
}
