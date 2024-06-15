using System.Linq;
using Content.Shared._CM14.Marines;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Damage;
using Content.Shared.DragDrop;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
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

    private readonly HashSet<EntityUid> _packsToUpdate = new();

    private EntityQuery<BloodPackComponent> _bloodPackQuery;

    public override void Initialize()
    {
        _bloodPackQuery = GetEntityQuery<BloodPackComponent>();

        SubscribeLocalEvent<IVDripComponent, EntInsertedIntoContainerMessage>(OnIVDripEntInserted);
        SubscribeLocalEvent<IVDripComponent, EntRemovedFromContainerMessage>(OnIVDripEntRemoved);
        SubscribeLocalEvent<IVDripComponent, AfterAutoHandleStateEvent>(OnIVDripAfterHandleState);
        SubscribeLocalEvent<IVDripComponent, CanDragEvent>(OnIVDripCanDrag);
        SubscribeLocalEvent<IVDripComponent, CanDropDraggedEvent>(OnIVDripCanDropDragged);
        SubscribeLocalEvent<IVDripComponent, DragDropDraggedEvent>(OnIVDripDragDropDragged);
        SubscribeLocalEvent<IVDripComponent, InteractHandEvent>(OnIVInteractHand);
        SubscribeLocalEvent<IVDripComponent, GetVerbsEvent<InteractionVerb>>(OnIVVerbs);
        SubscribeLocalEvent<IVDripComponent, ExaminedEvent>(OnIVExamine);

        // TODO CM14 check for BloodstreamComponent instead of MarineComponent
        SubscribeLocalEvent<MarineComponent, CanDropTargetEvent>(OnMarineCanDropTarget);

        SubscribeLocalEvent<BloodPackComponent, MapInitEvent>(OnBloodPackMapInit);
        SubscribeLocalEvent<BloodPackComponent, AfterAutoHandleStateEvent>(OnBloodPackAfterState);
        SubscribeLocalEvent<BloodPackComponent, SolutionContainerChangedEvent>(OnBloodPackSolutionChanged);
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
            Detach(iv, args.User, false, true);
    }

    private void OnIVInteractHand(Entity<IVDripComponent> iv, ref InteractHandEvent args)
    {
        Detach(iv, args.User, false, true);
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

    private void OnIVExamine(Entity<IVDripComponent> ent, ref ExaminedEvent args)
    {
        using (args.PushGroup(nameof(IVDripComponent)))
        {
            var injectingMsg = ent.Comp.Injecting
                ? "cm-iv-examine-injecting"
                : "cm-iv-examine-drawing";
            args.PushMarkup(Loc.GetString(injectingMsg, ("iv", ent.Owner)));

            var chemicalsMsg = Loc.GetString("cm-iv-examine-chemicals-none");
            if (_containers.TryGetContainer(ent, ent.Comp.Slot, out var container) &&
                container.ContainedEntities.FirstOrDefault() is { Valid: true } packId &&
                TryComp(packId, out BloodPackComponent? pack) &&
                _solutionContainer.TryGetSolution(packId, pack.Solution, out _, out var solution))
            {
                chemicalsMsg = Loc.GetString("cm-iv-examine-chemicals",
                    ("attached", packId),
                    ("units", solution.Volume.Int()));
            }

            args.PushMarkup(chemicalsMsg);

            var attachedMsg = ent.Comp.AttachedTo is { } attached
                ? Loc.GetString("cm-iv-examine-attached", ("attached", attached))
                : Loc.GetString("cm-iv-examine-attached-none");
            args.PushMarkup(attachedMsg);
        }
    }

    private void OnBloodPackMapInit(Entity<BloodPackComponent> pack, ref MapInitEvent args)
    {
        _packsToUpdate.Add(pack);
    }

    private void OnBloodPackAfterState(Entity<BloodPackComponent> pack, ref AfterAutoHandleStateEvent args)
    {
        UpdatePackVisuals(pack);
    }

    private void OnBloodPackSolutionChanged(Entity<BloodPackComponent> pack, ref SolutionContainerChangedEvent args)
    {
        UpdatePackVisuals(pack);
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

        var selfMessage = "cm-iv-attach-self-drawing";
        var othersMessage = "cm-iv-attach-others-drawing";
        if (iv.Comp.Injecting)
        {
            selfMessage = "cm-iv-attach-self-injecting";
            othersMessage = "cm-iv-attach-others-injecting";
        }

        _popup.PopupClient(Loc.GetString(selfMessage, ("target", to)), to, user);

        var others = Filter.PvsExcept(user);
        _popup.PopupEntity(Loc.GetString(othersMessage, ("user", user), ("target", to)), to, others, true);
    }

    protected void Detach(Entity<IVDripComponent> iv, EntityUid? user, bool rip, bool predict)
    {
        if (iv.Comp.AttachedTo is not { } target)
            return;

        iv.Comp.AttachedTo = default;
        Dirty(iv);

        if (rip)
        {
            if (iv.Comp.RipDamage != null)
                _damageable.TryChangeDamage(target, iv.Comp.RipDamage, true);

            if (!_timing.IsFirstTimePredicted)
                return;

            var message = Loc.GetString("cm-iv-rip", ("target", target));
            if (predict)
            {
                _popup.PopupClient(message, target, user);

                var others = Filter.PvsExcept(target);
                _popup.PopupEntity(message, target, others, true);
            }
            else
            {
                _popup.PopupEntity(message, target);
            }

            DoRip(iv, target);
        }
        else
        {
            if (!_timing.IsFirstTimePredicted)
                return;

            var selfMessage = Loc.GetString("cm-iv-detach-self", ("target", target));
            if (predict)
                _popup.PopupClient(selfMessage, target, user);
            else
                _popup.PopupEntity(selfMessage, target);

            if (user != null)
            {
                var others = Filter.PvsExcept(user.Value);
                _popup.PopupEntity(Loc.GetString("cm-iv-detach-others", ("user", user), ("target", target)),
                    target,
                    others,
                    true);
            }
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

    protected void UpdatePackVisuals(Entity<BloodPackComponent> pack)
    {
        if (!_solutionContainer.TryGetSolution(pack.Owner, pack.Comp.Solution, out _, out var solution))
        {
            UpdatePackAppearance(pack);
            return;
        }

        if (_containers.TryGetContainingContainer((pack, null), out var container) &&
            TryComp(container.Owner, out IVDripComponent? iv))
        {
            iv.FillColor = solution.GetColor(_prototype);
            iv.FillPercentage = (int) (solution.Volume / solution.MaxVolume * 100);
            Dirty(container.Owner, iv);
            UpdateIVAppearance((container.Owner, iv));
        }

        UpdatePackAppearance(pack);
    }

    protected void UpdateIVVisuals(Entity<IVDripComponent> iv)
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
        if (_net.IsClient)
            return;

        if (_solutionContainer.TryGetSolution(pack.Owner, pack.Comp.Solution, out var solEnt))
        {
            var solution = solEnt.Value.Comp.Solution;
            pack.Comp.FillPercentage = solution.Volume / solution.MaxVolume;
            pack.Comp.FillColor = solution.GetColor(_prototype);
        }
        else
        {
            pack.Comp.FillPercentage = FixedPoint2.Zero;
            pack.Comp.FillColor = Color.Transparent;
        }

        Dirty(pack);
    }

    protected virtual void DoRip(Entity<IVDripComponent> iv, EntityUid attached)
    {
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        foreach (var pack in _packsToUpdate)
        {
            if (_bloodPackQuery.TryComp(pack, out var comp))
                UpdatePackVisuals((pack, comp));
        }

        _packsToUpdate.Clear();
    }
}
