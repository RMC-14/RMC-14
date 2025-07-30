using Content.Shared._RMC14.Chemistry;
using Content.Shared._RMC14.Map;
using Content.Shared._RMC14.Rules;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Whitelist;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Prototypes;
using Content.Shared.Verbs;
using Content.Shared.DoAfter;

namespace Content.Shared._RMC14.Medical.Refill;

public sealed class CMRefillableSolutionSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly RMCMapSystem _rmcMap = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;
    [Dependency] private readonly SolutionTransferSystem _solutionTransfer = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly RMCPlanetSystem _rmcPlanet = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SharedDoAfterSystem _doafter = default!;

    private bool _log;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CMRefillableSolutionComponent, ExaminedEvent>(OnRefillableSolutionExamined);

        SubscribeLocalEvent<CMSolutionRefillerComponent, MapInitEvent>(OnRefillerMapInit);
        SubscribeLocalEvent<CMSolutionRefillerComponent, InteractUsingEvent>(OnRefillerInteractUsing);

        SubscribeLocalEvent<RMCRefillSolutionOnStoreComponent, EntInsertedIntoContainerMessage>(OnRefillSolutionOnStoreInserted);

        SubscribeLocalEvent<RMCRefillSolutionFromContainerOnStoreComponent, EntInsertedIntoContainerMessage>(OnRefillSolutionFromContainerOnStoreInserted);
        SubscribeLocalEvent<RMCRefillSolutionFromContainerOnStoreComponent, GetVerbsEvent<AlternativeVerb>>(OnRefillSolutionFromContainerOnStoreGetVerbs);
        SubscribeLocalEvent<RMCRefillSolutionFromContainerOnStoreComponent, ContainerFlushDoAfterEvent>(OnRefillSolutionFromContainerOnStoreFlush);

        SubscribeLocalEvent<RMCPressurizedSolutionComponent, AfterInteractEvent>(OnPressurizedRefillAttempt);
    }

    private void OnRefillableSolutionExamined(Entity<CMRefillableSolutionComponent> ent, ref ExaminedEvent args)
    {
        using (args.PushGroup(nameof(CMRefillableSolutionComponent)))
        {
            args.PushMarkup("[color=cyan]This can be refilled by clicking on a medical vendor with it![/color]");
        }
    }

    private void OnRefillerMapInit(Entity<CMSolutionRefillerComponent> ent, ref MapInitEvent args)
    {
        var transform = Transform(ent.Owner);

        if (ent.Comp.RandomizeReagentsPlanetside && _rmcPlanet.IsOnPlanet(transform))
        {
            // Random interval of 25 for reagents
            var amount = _random.NextDouble(0, ent.Comp.Max.Double() * 0.04) * 25;
            ent.Comp.Current = FixedPoint2.New(amount);
            Dirty(ent);
        }
    }

    private void OnRefillerInteractUsing(Entity<CMSolutionRefillerComponent> ent, ref InteractUsingEvent args)
    {
        var fillable = args.Used;
        if (TryComp<RMCHyposprayComponent>(args.Used, out var hypo) &&
            _container.TryGetContainer(args.Used, hypo.SlotId, out var container) &&
            container.ContainedEntities.Count != 0)
        {
            fillable = container.ContainedEntities[0];
        }

        if (!TryComp(fillable, out CMRefillableSolutionComponent? refillable))
            return;

        args.Handled = true;
        if (!_whitelist.IsValid(ent.Comp.Whitelist, fillable))
        {
            _popup.PopupClient(Loc.GetString("cm-refillable-solution-cannot-refill", ("user", ent.Owner), ("target", fillable)), args.User, args.User, PopupType.SmallCaution);
            return;
        }

        if (!_solution.TryGetSolution(fillable, refillable.Solution, out var solution))
            return;

        var solutionComp = solution.Value.Comp.Solution;
        if (solutionComp.AvailableVolume == FixedPoint2.Zero)
        {
            _popup.PopupClient(Loc.GetString("cm-refillable-solution-full", ("target", fillable)), args.User, args.User);
            return;
        }

        var anyRefilled = false;
        foreach (var (reagent, amount) in refillable.Reagents)
        {
            if (!ent.Comp.Reagents.Contains(reagent))
                continue;

            var refill = FixedPoint2.Min(ent.Comp.Current, amount);
            refill = FixedPoint2.Min(refill, solutionComp.AvailableVolume);
            if (refill == FixedPoint2.Zero)
                break;

            ent.Comp.Current -= refill;
            _solution.TryAddReagent(solution.Value, reagent, refill);
            anyRefilled = true;
        }

        if (anyRefilled)
        {
            Dirty(ent);
            var ev = new RefilledSolutionEvent();
            RaiseLocalEvent(args.Used, ref ev);
            _popup.PopupClient(Loc.GetString("cm-refillable-solution-whirring-noise", ("user", ent.Owner), ("target", fillable)), args.User, args.User);
        }
        else
        {
            _popup.PopupClient(Loc.GetString("cm-refillable-solution-cannot-refill", ("user", ent.Owner), ("target", fillable)), args.User, args.User, PopupType.SmallCaution);
        }
    }

    private void OnRefillSolutionOnStoreInserted(Entity<RMCRefillSolutionOnStoreComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (!_solution.TryGetSolution(ent.Owner, ent.Comp.SolutionId, out var solutionEnt) ||
            !TryGetStorageFillableSolution(args.Entity, out var refillable, out _))
        {
            return;
        }

        var volume = refillable.Value.Comp.Solution.AvailableVolume;
        _solutionTransfer.Transfer(null, ent, solutionEnt.Value, args.Entity, refillable.Value, volume);
    }

    private void OnRefillSolutionFromContainerOnStoreInserted(Entity<RMCRefillSolutionFromContainerOnStoreComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (!_container.TryGetContainer(ent, ent.Comp.ContainerId, out var container) ||
            !container.ContainedEntities.TryFirstOrNull(out var contained))
        {
            return;
        }

        if (!_solution.TryGetDrainableSolution(contained.Value, out var drainable, out var sol) && !TryGetPressurizedSolution(contained.Value, out drainable, out sol))
        {
            return;
        }

        if (sol != null)
            _appearance.SetData(ent, SolutionContainerStoreVisuals.Color, sol.GetColor(_proto));

        if (!TryGetStorageFillableSolution(args.Entity, out var refillable, out _))
        {
            return;
        }

        var volume = refillable.Value.Comp.Solution.AvailableVolume;
        var drained = _solution.SplitSolution(drainable.Value, volume);
        _solution.AddSolution(refillable.Value, drained);
    }

    public override void Update(float frameTime)
    {
        if (_net.IsClient)
            return;

        if (_log)
            Log.Info($"Running {nameof(CMRefillableSolutionSystem)}");

        var time = _timing.CurTime;
        var refillers = EntityQueryEnumerator<CMSolutionRefillerComponent, TransformComponent>();
        while (refillers.MoveNext(out var uid, out var comp, out var xform))
        {
            if (_log)
                Log.Info($"Running {nameof(CMRefillableSolutionSystem)} for {uid}: {time}, {comp.RechargeAt}");

            if (time < comp.RechargeAt)
                continue;

            comp.RechargeAt = time + comp.RechargeCooldown;
            Dirty(uid, comp);

            if (!xform.Anchored)
                continue;

            var anchored = _rmcMap.GetAnchoredEntitiesEnumerator(uid);
            var any = false;
            while (anchored.MoveNext(out var anchoredId))
            {
                if (HasComp<CMMedicalSupplyLinkComponent>(anchoredId))
                {
                    any = true;
                    break;
                }
            }

            if (!any)
                return;

            comp.Current = FixedPoint2.Min(comp.Max, comp.Current + comp.Recharge);
        }
    }

    public bool TryGetStorageFillableSolution(Entity<SolutionStorageFillableComponent?, SolutionContainerManagerComponent?> entity, [NotNullWhen(true)] out Entity<SolutionComponent>? soln, [NotNullWhen(true)] out Solution? solution)
    {
        if (!Resolve(entity, ref entity.Comp1, logMissing: false))
        {
            (soln, solution) = (default!, null);
            return false;
        }

        return _solution.TryGetSolution((entity.Owner, entity.Comp2), entity.Comp1.Solution, out soln, out solution);
    }

    public bool TryGetPressurizedSolution(Entity<RMCPressurizedSolutionComponent?, SolutionContainerManagerComponent?> entity, [NotNullWhen(true)] out Entity<SolutionComponent>? soln, [NotNullWhen(true)] out Solution? solution)
    {
        if (!Resolve(entity, ref entity.Comp1, logMissing: false))
        {
            (soln, solution) = (default!, null);
            return false;
        }

        return _solution.TryGetSolution((entity.Owner, entity.Comp2), entity.Comp1.Solution, out soln, out solution);
    }

    private void OnPressurizedRefillAttempt(Entity<RMCPressurizedSolutionComponent> beaker, ref AfterInteractEvent args)
    {
        if (!args.CanReach || args.Target is not { } target)
            return;

        var (uid, comp) = beaker;

        //Note below is the solutiontransfer code

        if (!HasComp<ReagentTankComponent>(target) || !TryComp<SolutionTransferComponent>(beaker, out var transfer))
            return;

        //Special case for reagent tanks, because normally clicking another container will give solution, not take it.
        if (!HasComp<RefillableSolutionComponent>(target) // target must not be refillable (e.g. Reagent Tanks)
            && _solution.TryGetDrainableSolution(target, out var targetSoln, out _) // target must be drainable
            && TryGetPressurizedSolution((uid, null, null), out var ownerSoln, out var ownerRefill))
        {
            var transferAmount = transfer.TransferAmount; // This is the player-configurable transfer amount of "uid," not the target reagent tank.

            var transferred = _solutionTransfer.Transfer(args.User, target, targetSoln.Value, uid, ownerSoln.Value, transferAmount);
            args.Handled = true;
            if (transferred > 0)
            {
                var toTheBrim = ownerRefill.AvailableVolume == 0;
                var msg = toTheBrim
                    ? "comp-solution-transfer-fill-fully"
                    : "comp-solution-transfer-fill-normal";

                _popup.PopupPredicted(Loc.GetString(msg, ("owner", args.Target), ("amount", transferred), ("target", uid)), uid, args.User);
                return;
            }
        }

        // if target is refillable, and owner is drainable
        if (TryComp<RefillableSolutionComponent>(target, out var targetRefill)
            && _solution.TryGetRefillableSolution((target, targetRefill, null), out targetSoln, out _)
            && TryGetPressurizedSolution(uid, out ownerSoln, out _))
        {
            var transferAmount = transfer.TransferAmount;

            if (targetRefill?.MaxRefill is { } maxRefill)
                transferAmount = FixedPoint2.Min(transferAmount, maxRefill);

            var transferred = _solutionTransfer.Transfer(args.User, uid, ownerSoln.Value, target, targetSoln.Value, transferAmount);
            args.Handled = true;
            if (transferred > 0)
            {
                var message = Loc.GetString("comp-solution-transfer-transfer-solution", ("amount", transferred), ("target", target));
                _popup.PopupEntity(message, uid, args.User);
            }
        }
    }

    private void OnRefillSolutionFromContainerOnStoreGetVerbs(Entity<RMCRefillSolutionFromContainerOnStoreComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!ent.Comp.CanFlush)
            return;

        if (!_container.TryGetContainer(ent, ent.Comp.ContainerId, out var container) ||
    !container.ContainedEntities.TryFirstOrNull(out var contained))
        {
            return;
        }

        EntityUid user = args.User;

        args.Verbs.Add(new AlternativeVerb
        {
            Text = Loc.GetString("rmc-refillsolution-flush"),
            Act = () =>
            {
                TryFlushContainer(ent, user);
            },
        });
    }

    private void TryFlushContainer(Entity<RMCRefillSolutionFromContainerOnStoreComponent> ent, EntityUid user)
    {
        if (!ent.Comp.CanFlush)
            return;

        if (!_container.TryGetContainer(ent, ent.Comp.ContainerId, out var container) ||
!container.ContainedEntities.TryFirstOrNull(out var contained))
        {
            return;
        }

        if (!_solution.TryGetDrainableSolution(contained.Value, out var drainable, out var sol) && !TryGetPressurizedSolution(contained.Value, out drainable, out sol))
        {
            return;
        }
        //TODO RMC immovable
        _popup.PopupClient(Loc.GetString("rmc-refillsolution-flush-start", ("time", ent.Comp.FlushTime.TotalSeconds)), user, user, PopupType.SmallCaution);
        _doafter.TryStartDoAfter(new DoAfterArgs(EntityManager, user, ent.Comp.FlushTime, new ContainerFlushDoAfterEvent(), ent, target: ent)
        {
            BreakOnMove = true,
            DuplicateCondition = DuplicateConditions.SameTarget,

        });
    }

    private void OnRefillSolutionFromContainerOnStoreFlush(Entity<RMCRefillSolutionFromContainerOnStoreComponent> ent, ref ContainerFlushDoAfterEvent args)
    {
        if (!ent.Comp.CanFlush)
            return;

        if (args.Cancelled || args.Handled)
            return;

        args.Handled = true;

        if (!_container.TryGetContainer(ent, ent.Comp.ContainerId, out var container) ||
!container.ContainedEntities.TryFirstOrNull(out var contained))
        {
            return;
        }

        if (!_solution.TryGetDrainableSolution(contained.Value, out var drainable, out var sol) && !TryGetPressurizedSolution(contained.Value, out drainable, out sol))
        {
            return;
        }

        _solution.RemoveAllSolution(drainable.Value);

        if (TryComp<AppearanceComponent>(ent, out var appearance))
            _appearance.QueueUpdate(ent, appearance);
    }
}
