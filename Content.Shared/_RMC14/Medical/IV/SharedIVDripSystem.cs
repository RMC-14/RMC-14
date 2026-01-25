using System.Linq;
using Content.Shared._RMC14.Marines.Skills;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.DragDrop;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Hands;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.PowerCell;
using Content.Shared.Verbs;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Medical.IV;

public abstract class SharedIVDripSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedContainerSystem _containers = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SkillsSystem _skills = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedPowerCellSystem _powerCell = default!;

    private readonly HashSet<EntityUid> _packsToUpdate = [];

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

        SubscribeLocalEvent<IVDripTargetComponent, CanDropTargetEvent>(OnIVTargetCanDropTarget);

        SubscribeLocalEvent<BloodPackComponent, MapInitEvent>(OnBloodPackMapInit);
        SubscribeLocalEvent<BloodPackComponent, AfterAutoHandleStateEvent>(OnBloodPackAfterState);
        SubscribeLocalEvent<BloodPackComponent, SolutionContainerChangedEvent>(OnBloodPackSolutionChanged);
        SubscribeLocalEvent<BloodPackComponent, AfterInteractEvent>(OnBloodPackAfterInteract);
        SubscribeLocalEvent<BloodPackComponent, AttachBloodPackDoAfterEvent>(OnBloodPackAttachDoAfter);
        SubscribeLocalEvent<BloodPackComponent, GotUnequippedHandEvent>(OnBloodPackUnequippedHand);
        SubscribeLocalEvent<BloodPackComponent, GetVerbsEvent<InteractionVerb>>(OnBloodPackVerbs);
        SubscribeLocalEvent<BloodPackComponent, ExaminedEvent>(OnBloodPackExamine);

        SubscribeLocalEvent<PortableDialysisComponent, AfterInteractEvent>(OnDialysisAfterInteract);
        SubscribeLocalEvent<PortableDialysisComponent, AfterAutoHandleStateEvent>(OnDialysisAfterHandleState);
        SubscribeLocalEvent<PortableDialysisComponent, AttachDialysisDoAfterEvent>(OnDialysisDoAfter);
        SubscribeLocalEvent<PortableDialysisComponent, GotUnequippedHandEvent>(OnDialysisUnequippedHand);
        SubscribeLocalEvent<PortableDialysisComponent, ExaminedEvent>(OnDialysisExamine);
        SubscribeLocalEvent<PortableDialysisComponent, PowerCellSlotEmptyEvent>(OnDialysisPowerEmpty);
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
        if (!HasComp<IVDripTargetComponent>(args.Target) || !InRange(iv, args.Target, iv.Comp.Range))
            return;
        args.Handled = true;
        args.CanDrop = true;
    }

    private void OnIVTargetCanDropTarget(Entity<IVDripTargetComponent> marine, ref CanDropTargetEvent args)
    {
        var iv = args.Dragged;
        if (!TryComp(iv, out IVDripComponent? ivComp) || !InRange(iv, marine, ivComp.Range))
            return;
        args.Handled = true;
        args.CanDrop = true;
    }

    private void OnIVDripDragDropDragged(Entity<IVDripComponent> iv, ref DragDropDraggedEvent args)
    {
        if (args.Handled)
            return;

        if (iv.Comp.AttachedTo == default)
            AttachIV(iv, args.User, args.Target);
        else
            DetachIV(iv, args.User, false, true);
    }

    private void OnIVInteractHand(Entity<IVDripComponent> iv, ref InteractHandEvent args)
    {
        DetachIV(iv, args.User, false, true);
    }

    private void OnIVVerbs(Entity<IVDripComponent> iv, ref GetVerbsEvent<InteractionVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        var user = args.User;
        args.Verbs.Add(new InteractionVerb
        {
            Act = () => ToggleInject(iv, user),
            Text = Loc.GetString("cm-iv-verb-toggle-inject"),
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

    private void OnBloodPackAfterInteract(Entity<BloodPackComponent> pack, ref AfterInteractEvent args)
    {
        if (args.Target is not { } target)
            return;

        if (!InRange(pack, target, pack.Comp.Range) || !HasComp<IVDripTargetComponent>(target))
            return;

        args.Handled = true;

        var user = args.User;
        if (pack.Comp.AttachedTo != null)
        {
            DetachPack((pack, pack), user, false, true);
            return;
        }

        if (!_skills.HasAllSkills(user, pack.Comp.SkillRequired))
        {
            _popup.PopupClient(Loc.GetString("cm-iv-attach-no-skill"), user, user);
            return;
        }

        if (user == target)
        {
            _popup.PopupClient(Loc.GetString("cm-blood-pack-cannot-self"), user, user);
            return;
        }

        var delay = pack.Comp.AttachDelay;
        if (delay > TimeSpan.Zero)
        {
            var selfPoke = Loc.GetString("cm-blood-pack-poke-self", ("pack", pack.Owner), ("target", target));
            var othersPoke = Loc.GetString("cm-blood-pack-poke-others",
                ("user", user),
                ("pack", pack.Owner),
                ("target", target));
            _popup.PopupPredicted(selfPoke, othersPoke, target, user);
        }

        var ev = new AttachBloodPackDoAfterEvent();
        var doAfter = new DoAfterArgs(EntityManager, user, delay, ev, pack, target, pack)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            BreakOnHandChange = true,
            BlockDuplicate = true,
            DuplicateCondition = DuplicateConditions.SameEvent,
            TargetEffect = "RMCEffectHealBusy",
        };
        _doAfter.TryStartDoAfter(doAfter);
    }

    private void OnBloodPackAttachDoAfter(Entity<BloodPackComponent> pack, ref AttachBloodPackDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Target is not { } target)
            return;

        AttachPack(pack, args.User, target);
    }

    private void OnBloodPackUnequippedHand(Entity<BloodPackComponent> pack, ref GotUnequippedHandEvent args)
    {
        DetachPack((pack, pack), args.User, true, true);
    }

    private void OnBloodPackVerbs(Entity<BloodPackComponent> pack, ref GetVerbsEvent<InteractionVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        var user = args.User;
        args.Verbs.Add(new InteractionVerb
        {
            Act = () => ToggleInject(pack, user),
            Text = Loc.GetString("cm-iv-verb-toggle-inject"),
        });
    }

    private void OnBloodPackExamine(Entity<BloodPackComponent> pack, ref ExaminedEvent args)
    {
        using (args.PushGroup(nameof(BloodPackComponent)))
        {
            var injectingMsg = pack.Comp.Injecting
                ? "cm-iv-examine-injecting"
                : "cm-iv-examine-drawing";
            args.PushMarkup(Loc.GetString(injectingMsg, ("iv", pack.Owner)));

            var attachedMsg = pack.Comp.AttachedTo is { } attached
                ? Loc.GetString("cm-iv-examine-attached", ("attached", attached))
                : Loc.GetString("cm-iv-examine-attached-none");
            args.PushMarkup(attachedMsg);

            if (_solutionContainer.TryGetSolution(pack.Owner, pack.Comp.Solution, out _, out var solution))
                args.PushMarkup(Loc.GetString("cm-blood-pack-contains", ("units", solution.Volume.Int())));
        }
    }

    private void OnDialysisAfterInteract(Entity<PortableDialysisComponent> dialysis, ref AfterInteractEvent args)
    {
        if (args.Target is not { } target)
            return;

        if (!InRange(dialysis, target, dialysis.Comp.Range) || !HasComp<IVDripTargetComponent>(target))
            return;

        args.Handled = true;

        var user = args.User;
        if (dialysis.Comp.AttachedTo != null)
        {
            DetachDialysis(dialysis, args.User, false, true);
            return;
        }

        if (!_skills.HasAllSkills(user, dialysis.Comp.SkillRequired))
        {
            _popup.PopupClient(Loc.GetString("cm-iv-attach-no-skill"), user, user);
            return;
        }

        if (user == target)
        {
            _popup.PopupClient(Loc.GetString("cm-blood-pack-cannot-self"), user, user);
            return;
        }

        var delay = dialysis.Comp.AttachDelay;
        if (delay > TimeSpan.Zero)
        {
            var selfPoke = Loc.GetString("cm-blood-pack-poke-self", ("pack", dialysis.Owner), ("target", target));
            var othersPoke = Loc.GetString("cm-blood-pack-poke-others",
                ("user", user),
                ("pack", dialysis.Owner),
                ("target", target));
            _popup.PopupPredicted(selfPoke, othersPoke, target, user);
        }

        dialysis.Comp.IsAttaching = true;
        Dirty(dialysis);
        UpdateDialysisVisuals(dialysis);

        var ev = new AttachDialysisDoAfterEvent();
        var doAfter = new DoAfterArgs(EntityManager, user, delay, ev, dialysis, target, dialysis)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            BreakOnHandChange = true,
            BlockDuplicate = true,
            DuplicateCondition = DuplicateConditions.SameEvent,
            TargetEffect = "RMCEffectHealBusy",
        };
        _doAfter.TryStartDoAfter(doAfter);
    }

    private void OnDialysisDoAfter(Entity<PortableDialysisComponent> dialysis, ref AttachDialysisDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Target is not { } target)
        {
            dialysis.Comp.IsAttaching = false;
            dialysis.Comp.IsDetaching = false;
            Dirty(dialysis);
            UpdateDialysisVisuals(dialysis);
            return;
        }

        dialysis.Comp.IsAttaching = false;
        AttachDialysis(dialysis, args.User, target);
    }

    private void OnDialysisExamine(Entity<PortableDialysisComponent> dialysis, ref ExaminedEvent args)
    {
        using (args.PushGroup(nameof(PortableDialysisComponent)))
        {
            var attachedMsg = dialysis.Comp.AttachedTo is { } attached
                ? Loc.GetString("cm-iv-examine-attached", ("attached", attached))
                : Loc.GetString("cm-iv-examine-attached-none");
            args.PushMarkup(attachedMsg);
        }
    }

    private void OnDialysisPowerEmpty(Entity<PortableDialysisComponent> dialysis, ref PowerCellSlotEmptyEvent args)
    {
        DetachDialysis(dialysis, null, true, true);
    }

    private void OnDialysisUnequippedHand(Entity<PortableDialysisComponent> dialysis, ref GotUnequippedHandEvent args)
    {
        DetachDialysis((dialysis, dialysis), args.User, true, true);
    }

    private void OnDialysisAfterHandleState(Entity<PortableDialysisComponent> dialysis, ref AfterAutoHandleStateEvent args)
    {
        UpdateDialysisVisuals(dialysis);
    }

    protected bool InRange(EntityUid iv, EntityUid to, float range)
    {
        var ivPos = _transform.GetMapCoordinates(iv);
        var toPos = _transform.GetMapCoordinates(to);
        return ivPos.InRange(toPos, range);
    }

    private void AttachIV(Entity<IVDripComponent> iv, EntityUid user, EntityUid to)
    {
        if (!InRange(iv, to, iv.Comp.Range))
            return;

        if (!_skills.HasAllSkills(user, iv.Comp.SkillRequired))
        {
            _popup.PopupClient(Loc.GetString("cm-iv-attach-no-skill"), user, user);
            return;
        }

        iv.Comp.AttachedTo = to;
        Dirty(iv);

        AttachFeedback(iv, user, to, iv.Comp.Injecting);
    }

    protected void DetachIV(Entity<IVDripComponent> iv, EntityUid? user, bool rip, bool predict)
    {
        if (iv.Comp.AttachedTo is not { } target)
            return;

        if (user != null && !_skills.HasAllSkills(user.Value, iv.Comp.SkillRequired))
        {
            _popup.PopupClient(Loc.GetString("cm-iv-detach-no-skill"), user.Value, user.Value);
            return;
        }

        iv.Comp.AttachedTo = default;
        Dirty(iv);

        if (rip)
            DoRip(iv.Comp.RipDamage, target, user, iv.Comp.RipEmote, predict);
        else
            DoDetachFeedback(iv, target, user, predict);
    }

    private void AttachPack(Entity<BloodPackComponent> pack, EntityUid user, EntityUid to)
    {
        if (!InRange(pack, to, pack.Comp.Range))
            return;

        if (!_skills.HasAllSkills(user, pack.Comp.SkillRequired))
        {
            _popup.PopupClient(Loc.GetString("cm-iv-attach-no-skill"), user, user);
            return;
        }

        pack.Comp.AttachedTo = to;
        Dirty(pack);

        AttachFeedback(pack, user, to, pack.Comp.Injecting);
    }

    protected void DetachPack(Entity<BloodPackComponent> pack, EntityUid? user, bool rip, bool predict)
    {
        if (pack.Comp.AttachedTo is not { } target)
            return;

        if (user != null && !_skills.HasAllSkills(user.Value, pack.Comp.SkillRequired))
        {
            _popup.PopupClient(Loc.GetString("cm-iv-detach-no-skill"), user.Value, user.Value);
            return;
        }

        pack.Comp.AttachedTo = default;
        Dirty(pack);

        if (rip)
            DoRip(pack.Comp.RipDamage, target, user, pack.Comp.RipEmote, predict);
        else
            DoDetachFeedback(pack, target, user, predict);
    }

    private void AttachDialysis(Entity<PortableDialysisComponent> dialysis, EntityUid user, EntityUid to)
    {
        if (!InRange(dialysis, to, dialysis.Comp.Range))
            return;

        if (!_skills.HasAllSkills(user, dialysis.Comp.SkillRequired))
        {
            _popup.PopupClient(Loc.GetString("cm-iv-attach-no-skill"), user, user);
            return;
        }

        dialysis.Comp.IsDetaching = false;
        dialysis.Comp.IsAttaching = false;
        dialysis.Comp.AttachedTo = to;
        Dirty(dialysis);
        UpdateDialysisVisuals(dialysis);

        _powerCell.SetDrawEnabled((dialysis.Owner, null), true);

        AttachFeedback(dialysis, user, to, false);
    }

    protected void DetachDialysis(Entity<PortableDialysisComponent> dialysis, EntityUid? user, bool rip, bool predict)
    {
        if (dialysis.Comp.AttachedTo is not { } target)
            return;

        if (user != null && !_skills.HasAllSkills(user.Value, dialysis.Comp.SkillRequired))
        {
            _popup.PopupClient(Loc.GetString("cm-iv-detach-no-skill"), user.Value, user.Value);
            return;
        }

        var uid = dialysis.Owner;
        dialysis.Comp.AttachedTo = default;
        dialysis.Comp.IsDetaching = true;
        Dirty(dialysis);
        UpdateDialysisVisuals(dialysis);
        OnServerDialysisDetached(dialysis);

        var delay = dialysis.Comp.AttachDelay;
        if (delay > TimeSpan.Zero)
        {
            var captured = uid;
            Timer.Spawn(
                delay,
                () =>
                {
                    if (!TryComp(captured, out PortableDialysisComponent? comp))
                        return;

                    if (!comp.IsDetaching || comp.AttachedTo != null)
                        return;
                    comp.IsDetaching = false;
                    Dirty(captured, comp);
                    UpdateDialysisVisuals((captured, comp));
                    OnServerDialysisDetached((captured, comp));
                }
            );
        }
        else
        {
            dialysis.Comp.IsDetaching = false;
            Dirty(dialysis);
            UpdateDialysisVisuals(dialysis);
            OnServerDialysisDetached(dialysis);
        }

        _powerCell.SetDrawEnabled((dialysis.Owner, null), false);

        if (rip)
            DoRip(dialysis.Comp.RipDamage, target, user, dialysis.Comp.RipEmote, predict);
        else
            DoDetachFeedback(dialysis, target, user, predict);
    }

    protected virtual void OnServerDialysisDetached(Entity<PortableDialysisComponent> dialysis)
    {
    }

    private void ToggleInject(Entity<IVDripComponent> iv, EntityUid user)
    {
        ToggleInject(iv, ref iv.Comp.Injecting, user);
        Dirty(iv);
    }

    private void ToggleInject(Entity<BloodPackComponent> pack, EntityUid user)
    {
        ToggleInject(pack, ref pack.Comp.Injecting, user);
        Dirty(pack);
    }

    private void ToggleInject(EntityUid iv, ref bool injecting, EntityUid user)
    {
        injecting = !injecting;

        var msg = injecting
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

        if (!_containers.TryGetContainer(iv, iv.Comp.Slot, out var container))
            return;

        foreach (var entity in container.ContainedEntities)
        {
            if (!TryComp(entity, out BloodPackComponent? pack) ||
                !_solutionContainer.TryGetSolution(entity, pack.Solution, out _, out var solution))
                continue;

            iv.Comp.FillColor = solution.GetColor(_prototype);
            iv.Comp.FillPercentage = (int) (solution.Volume / solution.MaxVolume * 100);
            Dirty(iv);
            UpdateIVAppearance(iv);
            return;
        }

        iv.Comp.FillColor = Color.White;
        iv.Comp.FillPercentage = 0;
        Dirty(iv);
        UpdateIVAppearance(iv);
    }

    protected void UpdateDialysisVisuals(Entity<PortableDialysisComponent> dialysis)
    {
        if (_net.IsClient)
        {
            UpdateDialysisAppearance(dialysis);
            return;
        }

        Dirty(dialysis);
        UpdateDialysisAppearance(dialysis);
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

    protected virtual void UpdateDialysisAppearance(Entity<PortableDialysisComponent> dialysis)
    {
    }

    protected void UpdateDialysisBatteryAppearance(Entity<AppearanceComponent?> ent, DialysisBatteryLevel batteryLevel)
    {
        _appearance.SetData(ent, DialysisVisuals.BatteryLevel, batteryLevel, ent.Comp);
    }

    protected virtual void DoRip(DamageSpecifier? damage,
        EntityUid attached,
        EntityUid? user,
        ProtoId<EmotePrototype> ripEmote,
        bool predict)
    {
        if (damage != null)
            _damageable.TryChangeDamage(attached, damage, true);

        if (!_timing.IsFirstTimePredicted)
            return;

        var message = Loc.GetString("cm-iv-rip", ("target", attached));
        if (predict)
        {
            _popup.PopupClient(message, attached, user);

            var others = user == null ? Filter.Pvs(attached) : Filter.PvsExcept(user.Value);
            _popup.PopupEntity(message, attached, others, true);
        }
        else
        {
            _popup.PopupEntity(message, attached);
        }
    }

    private void AttachFeedback(EntityUid iv, EntityUid user, EntityUid to, bool injecting)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        var selfMessage = "cm-iv-attach-self-drawing";
        var othersMessage = "cm-iv-attach-others-drawing";
        if (injecting)
        {
            selfMessage = "cm-iv-attach-self-injecting";
            othersMessage = "cm-iv-attach-others-injecting";
        }

        _popup.PopupClient(Loc.GetString(selfMessage, ("iv", iv), ("target", to)), to, user);

        var others = Filter.PvsExcept(user);
        _popup.PopupEntity(Loc.GetString(othersMessage, ("iv", iv), ("user", user), ("target", to)), to, others, true);
    }

    private void DoDetachFeedback(EntityUid iv, EntityUid attached, EntityUid? user, bool predict)
    {
        var selfMessage = Loc.GetString("cm-iv-detach-self", ("iv", iv), ("target", attached));
        if (predict)
            _popup.PopupClient(selfMessage, attached, user);
        else
            _popup.PopupEntity(selfMessage, attached);

        if (user == null)
            return;

        var others = Filter.PvsExcept(user.Value);
        _popup.PopupEntity(Loc.GetString("cm-iv-detach-others", ("iv", iv), ("user", user), ("target", attached)),
            attached,
            others,
            true);
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
