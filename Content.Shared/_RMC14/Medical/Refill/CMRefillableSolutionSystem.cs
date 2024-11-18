using Content.Shared._RMC14.Chemistry;
using Content.Shared._RMC14.Map;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Whitelist;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.Medical.Refill;

public sealed class CMRefillableSolutionSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedRMCMapSystem _rmcMap = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] protected readonly SharedContainerSystem _container = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CMRefillableSolutionComponent, ExaminedEvent>(OnRefillableSolutionExamined);

        SubscribeLocalEvent<CMSolutionRefillerComponent, InteractUsingEvent>(OnRefillerInteractUsing);

        SubscribeLocalEvent<RMCRefillSolutionOnStoreComponent, EntInsertedIntoContainerMessage>(OnRefillSolutionOnStoreInserted);
    }

    private void OnRefillableSolutionExamined(Entity<CMRefillableSolutionComponent> ent, ref ExaminedEvent args)
    {
        using (args.PushGroup(nameof(CMRefillableSolutionComponent)))
        {
            args.PushMarkup("[color=cyan]This can be refilled by clicking on a medical vendor with it![/color]");
        }
    }

    private void OnRefillerInteractUsing(Entity<CMSolutionRefillerComponent> ent, ref InteractUsingEvent args)
    {
        args.Handled = true;
        var fillable = args.Used;
        if(TryComp<RMCHyposprayComponent>(args.Used, out var hypo) && _container.TryGetContainer(args.Used, hypo.SlotId, out var container) && container.ContainedEntities.Count != 0)
        {
            fillable = container.ContainedEntities[0];
        }

        if (!TryComp(fillable, out CMRefillableSolutionComponent? refillable) ||
            !_whitelist.IsValid(ent.Comp.Whitelist, fillable))
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
        if (!_container.TryGetContainer(ent, ent.Comp.ContainerId, out var container) ||
            !container.ContainedEntities.TryFirstOrNull(out var contained))
        {
            return;
        }

        if (!_solution.TryGetDrainableSolution(contained.Value, out var drainable, out _) ||
            !_solution.TryGetRefillableSolution(args.Entity, out var refillable, out _))
        {
            return;
        }

        var volume = refillable.Value.Comp.Solution.AvailableVolume;
        var drained = _solution.Drain(contained.Value, drainable.Value, volume);
        _solution.Refill(args.Entity, refillable.Value, drained);
    }

    public override void Update(float frameTime)
    {
        if (_net.IsClient)
            return;

        var time = _timing.CurTime;
        var refillers = EntityQueryEnumerator<CMSolutionRefillerComponent, TransformComponent>();
        while (refillers.MoveNext(out var uid, out var comp, out var xform))
        {
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
}
