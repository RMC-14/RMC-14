using Content.Shared._CM14.Marines;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Damage;
using Content.Shared.DragDrop;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared._CM14.Medical.IV;

public abstract class SharedIVDripSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _containers = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<IVDripComponent, EntInsertedIntoContainerMessage>(OnIVDripEntInserted);
        SubscribeLocalEvent<IVDripComponent, EntRemovedFromContainerMessage>(OnIVDripEntRemoved);
        SubscribeLocalEvent<IVDripComponent, AfterAutoHandleStateEvent>(OnIVDripAfterHandleState);

        SubscribeLocalEvent<IVDripComponent, CanDragEvent>(OnIVDripCanDrag);
        SubscribeLocalEvent<IVDripComponent, CanDropDraggedEvent>(OnIVDripCanDropDragged);
        SubscribeLocalEvent<IVDripComponent, DragDropDraggedEvent>(OnIVDripDragDropDragged);
        SubscribeLocalEvent<IVDripComponent, InteractHandEvent>(OnIVInteractHand);
        SubscribeLocalEvent<IVDripComponent, GetVerbsEvent<InteractionVerb>>(OnIVVerbs);

        // TODO CM14 check for BloodstreamComponent instead of MarineComponent
        SubscribeLocalEvent<MarineComponent, CanDropTargetEvent>(OnMarineCanDropTarget);

        SubscribeLocalEvent<BloodPackComponent, MapInitEvent>(OnBloodPackMapInitEvent);
        SubscribeLocalEvent<BloodPackComponent, SolutionChangedEvent>(OnBloodPackSolutionChanged);
    }

    private void OnIVDripEntInserted(Entity<IVDripComponent> iv, ref EntInsertedIntoContainerMessage args)
    {
        UpdateIVVisuals(iv);
    }

    private void OnIVDripEntRemoved(Entity<IVDripComponent> iv, ref EntRemovedFromContainerMessage args)
    {
        UpdateIVVisuals(iv);
    }

    private void OnIVDripAfterHandleState(Entity<IVDripComponent> iv, ref AfterAutoHandleStateEvent args)
    {
        UpdateIVAppearance(iv);
    }

    private void OnIVDripCanDrag(Entity<IVDripComponent> iv, ref CanDragEvent args)
    {
        args.Handled = true;
    }

    private void OnIVDripCanDropDragged(Entity<IVDripComponent> iv, ref CanDropDraggedEvent args)
    {
        // TODO CM14 check for BloodstreamComponent instead of MarineComponent
        if (HasComp<MarineComponent>(args.Target) && InRange(iv, args.Target))
        {
            args.Handled = true;
            args.CanDrop = true;
        }
    }

    // TODO CM14 check for BloodstreamComponent instead of MarineComponent
    private void OnMarineCanDropTarget(Entity<MarineComponent> marine, ref CanDropTargetEvent args)
    {
        var iv = args.Dragged;
        if (TryComp(iv, out IVDripComponent? ivComp) && InRange((iv, ivComp), marine))
        {
            args.Handled = true;
            args.CanDrop = true;
        }
    }

    private void OnIVDripDragDropDragged(Entity<IVDripComponent> iv, ref DragDropDraggedEvent args)
    {
        if (args.Handled)
            return;

        if (iv.Comp.AttachedTo == default)
            Attach(iv, args.User, args.Target);
        else
            Detach(iv, false, true);
    }

    private void OnIVInteractHand(Entity<IVDripComponent> iv, ref InteractHandEvent args)
    {
        Detach(iv, false, true);
    }

    private void OnIVVerbs(Entity<IVDripComponent> iv, ref GetVerbsEvent<InteractionVerb> args)
    {
        var user = args.User;
        args.Verbs.Add(new InteractionVerb
        {
            Act = () => ToggleInject(iv, user),
            Text = Loc.GetString("cm-iv-verb-toggle-inject")
        });
    }

    private void OnBloodPackMapInitEvent(Entity<BloodPackComponent> pack, ref MapInitEvent args)
    {
        if (!_solutionContainer.TryGetSolution(pack.Owner, pack.Comp.Solution, out _, out var solution))
            return;

        UpdatePackVisuals(pack, solution);
    }

    private void OnBloodPackSolutionChanged(Entity<BloodPackComponent> pack, ref SolutionChangedEvent args)
    {
        UpdatePackVisuals(pack, args.Solution.Comp.Solution);
    }

    protected bool InRange(Entity<IVDripComponent> iv, EntityUid to)
    {
        var ivPos = _transform.GetMapCoordinates(iv);
        var toPos = _transform.GetMapCoordinates(to);
        return ivPos.InRange(toPos, iv.Comp.Range);
    }

    protected void Attach(Entity<IVDripComponent> iv, EntityUid user, EntityUid to)
    {
        if (!InRange(iv, to))
            return;

        iv.Comp.AttachedTo = to;
        Dirty(iv);

        if (!_timing.IsFirstTimePredicted)
            return;

        _popup.PopupClient(Loc.GetString("cm-iv-attach-self", ("target", to)), to, user);

        var others = Filter.PvsExcept(user);
        _popup.PopupEntity(Loc.GetString("cm-iv-attach-self", ("target", others)), to, others, true);
    }

    protected void Detach(Entity<IVDripComponent> iv, bool rip, bool predict)
    {
        if (iv.Comp.AttachedTo is not { } target)
            return;

        iv.Comp.AttachedTo = default;
        Dirty(iv);

        if (!_timing.IsFirstTimePredicted)
            return;

        if (rip)
        {
            if (iv.Comp.RipDamage != null)
                _damageable.TryChangeDamage(target, iv.Comp.RipDamage, true);

            var message = Loc.GetString("cm-iv-rip", ("target", target));
            if (predict)
            {
                _popup.PopupClient(message, target, target);

                var others = Filter.PvsExcept(target);
                _popup.PopupEntity(message, target, others, true);
            }
            else
            {
                _popup.PopupEntity(message, target);
            }
        }
        else
        {
            var selfMessage = Loc.GetString("cm-iv-detach-self", ("target", target));
            if (predict)
                _popup.PopupClient(selfMessage, target, target);
            else
                _popup.PopupEntity(selfMessage, target);

            var others = Filter.PvsExcept(target);
            _popup.PopupEntity(Loc.GetString("cm-iv-detach-others", ("target", others)), target, others, true);
        }
    }

    private void ToggleInject(Entity<IVDripComponent> iv, EntityUid user)
    {
        iv.Comp.Injecting = !iv.Comp.Injecting;
        Dirty(iv);

        var msg = iv.Comp.Injecting
            ? Loc.GetString("cm-iv-now-injecting")
            : Loc.GetString("cm-iv-now-taking");

        _popup.PopupClient(msg, iv, user);
    }

    private void UpdatePackVisuals(Entity<BloodPackComponent> pack, Solution solution)
    {
        if (_containers.TryGetContainingContainer(pack, out var container) &&
            TryComp(container.Owner, out IVDripComponent? iv))
        {
            iv.FillColor = solution.GetColor(_prototype);
            iv.FillPercentage = (int) (solution.Volume / solution.MaxVolume * 100);
            Dirty(container.Owner, iv);
            UpdateIVAppearance((container.Owner, iv));
        }

        UpdatePackAppearance(pack);
    }

    private void UpdateIVVisuals(Entity<IVDripComponent> iv)
    {
        // the client doesn't always know about solutions
        if (_net.IsClient)
        {
            UpdateIVAppearance(iv);
            return;
        }

        if (_containers.TryGetContainer(iv, iv.Comp.Slot, out var container))
        {
            foreach (var entity in container.ContainedEntities)
            {
                if (TryComp(entity, out BloodPackComponent? pack) &&
                    _solutionContainer.TryGetSolution(entity, pack.Solution, out _, out var solution))
                {
                    iv.Comp.FillColor = solution.GetColor(_prototype);
                    iv.Comp.FillPercentage = (int) (solution.Volume / solution.MaxVolume * 100);
                    Dirty(iv);
                    UpdateIVAppearance(iv);
                    return;
                }
            }

            iv.Comp.FillColor = Color.White;
            iv.Comp.FillPercentage = 0;
            Dirty(iv);
            UpdateIVAppearance(iv);
        }
    }

    protected virtual void UpdateIVAppearance(Entity<IVDripComponent> iv)
    {
    }

    protected virtual void UpdatePackAppearance(Entity<BloodPackComponent> pack)
    {
    }
}
