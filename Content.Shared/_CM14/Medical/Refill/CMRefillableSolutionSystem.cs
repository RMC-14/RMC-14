using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Whitelist;
using Robust.Shared.Map.Components;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared._CM14.Medical.Refill;

public sealed class CMRefillableSolutionSystem : EntitySystem
{
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CMSolutionRefillerComponent, InteractUsingEvent>(OnRefillerInteractUsing);
    }

    private void OnRefillerInteractUsing(Entity<CMSolutionRefillerComponent> ent, ref InteractUsingEvent args)
    {
        args.Handled = true;
        if (!TryComp(args.Used, out CMRefillableSolutionComponent? refillable) ||
            !_whitelist.IsValid(ent.Comp.Whitelist, args.Used))
        {
            _popup.PopupClient(Loc.GetString("cm-refillable-solution-cannot-refill", ("user", ent.Owner), ("target", args.Used)), args.User, args.User, PopupType.SmallCaution);
            return;
        }

        if (!_solution.TryGetSolution(args.Used, refillable.Solution, out var solution))
            return;

        var solutionComp = solution.Value.Comp.Solution;
        if (solutionComp.AvailableVolume == FixedPoint2.Zero)
        {
            _popup.PopupClient(Loc.GetString("cm-refillable-solution-full", ("target", args.Used)), args.User, args.User);
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
            _popup.PopupClient(Loc.GetString("cm-refillable-solution-whirring-noise", ("user", ent.Owner), ("target", args.Used)), args.User, args.User);
        }
        else
        {
            _popup.PopupClient(Loc.GetString("cm-refillable-solution-cannot-refill", ("user", ent.Owner), ("target", args.Used)), args.User, args.User, PopupType.SmallCaution);
        }
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

            if (!xform.Anchored ||
                !TryComp(xform.GridUid, out MapGridComponent? grid))
            {
                continue;
            }

            var position = _map.LocalToTile(xform.GridUid.Value, grid, xform.Coordinates);
            var anchored = _map.GetAnchoredEntitiesEnumerator(xform.GridUid.Value, grid, position);
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
