using Content.Shared._RMC14.Construction;
using Content.Shared._RMC14.Damage;
using Content.Shared._RMC14.Marines.Skills;
using Content.Shared._RMC14.Repairable;
using Content.Shared._RMC14.Xenonids;
using Content.Shared._RMC14.Xenonids.Acid;
using Content.Shared._RMC14.Xenonids.Spray;
using Content.Shared.ActionBlocker;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.DragDrop;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Content.Shared.Stacks;
using Content.Shared.Tools.Components;
using Content.Shared.Tools.Systems;
using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Barricade;

public sealed class RMCFoldingBarricadeSystem : EntitySystem
{
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly RMCConstructionSystem _construction = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly RMCRepairableSystem _repairable = default!;
    [Dependency] private readonly SharedRMCDamageableSystem _rmcDamageable = default!;
    [Dependency] private readonly SkillsSystem _skills = default!;
    [Dependency] private readonly SharedStackSystem _stack = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedToolSystem _tool = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private const string PryingQuality = "Prying";
    private const string WeldingQuality = "Welding";

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCFoldingBarricadeStackComponent, ComponentStartup>(OnStackStartup);
        SubscribeLocalEvent<RMCFoldingBarricadeStackComponent, StackCountChangedEvent>(OnStackCountChanged);
        SubscribeLocalEvent<RMCFoldingBarricadeStackComponent, StackSplitEvent>(OnStackSplit);
        SubscribeLocalEvent<RMCFoldingBarricadeStackComponent, UseInHandEvent>(OnStackUseInHand);
        SubscribeLocalEvent<RMCFoldingBarricadeStackComponent, InteractUsingEvent>(OnStackInteractUsing, before: [typeof(SharedStackSystem)]);
        SubscribeLocalEvent<RMCFoldingBarricadeStackComponent, RMCFoldingBarricadeDeployDoAfterEvent>(OnStackDeployDoAfter);
        SubscribeLocalEvent<RMCFoldingBarricadeStackComponent, RMCFoldingBarricadeRepairDoAfterEvent>(OnStackRepairDoAfter);
        SubscribeLocalEvent<RMCFoldingBarricadeStackComponent, ExaminedEvent>(OnStackExamined);

        SubscribeLocalEvent<RMCFoldingBarricadeComponent, InteractUsingEvent>(OnBarricadeInteractUsing, before: [typeof(SharedStackSystem)]);
        SubscribeLocalEvent<RMCFoldingBarricadeComponent, GetVerbsEvent<AlternativeVerb>>(OnBarricadeGetAlternativeVerbs);
        SubscribeLocalEvent<RMCFoldingBarricadeComponent, CanDragEvent>(OnBarricadeCanDrag);
        SubscribeLocalEvent<RMCFoldingBarricadeComponent, CanDropDraggedEvent>(OnBarricadeCanDropDragged);
        SubscribeLocalEvent<RMCFoldingBarricadeComponent, DragDropDraggedEvent>(OnBarricadeDragDropDragged);
        SubscribeLocalEvent<RMCFoldingBarricadeComponent, RMCFoldingBarricadeCollapseDoAfterEvent>(OnBarricadeCollapseDoAfter);
        SubscribeLocalEvent<RMCFoldingBarricadeComponent, ExaminedEvent>(OnBarricadeExamined);
    }

    private void OnStackStartup(Entity<RMCFoldingBarricadeStackComponent> ent, ref ComponentStartup args)
    {
        NormalizeStoredDamage(ent);
    }

    private void OnStackUseInHand(Entity<RMCFoldingBarricadeStackComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        TryStartDeploy(ent, args.User);
    }

    private void OnStackInteractUsing(Entity<RMCFoldingBarricadeStackComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (TryComp(args.Used, out RMCFoldingBarricadeStackComponent? usedFolding))
        {
            args.Handled = true;
            TryMergeStacks((args.Used, usedFolding), ent, args.User, args.ClickLocation);
            return;
        }

        if (!_tool.HasQuality(args.Used, WeldingQuality))
            return;

        args.Handled = true;
        TryStartStackRepair(ent, args.User, args.Used);
    }

    private void OnStackDeployDoAfter(Entity<RMCFoldingBarricadeStackComponent> ent, ref RMCFoldingBarricadeDeployDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        args.Handled = true;

        if (!_net.IsServer)
            return;

        if (!TryComp(ent, out StackComponent? stack) || stack.Count <= 0)
            return;

        if (!CanDeploy(ent, args.User, out var direction, out var popup))
        {
            if (popup != null)
                _popup.PopupEntity(popup, ent, args.User, PopupType.SmallCaution);

            return;
        }

        var damage = PopStoredDamage(ent, stack);
        SetCountPreserving(ent, stack.Count - 1, stack);

        var coords = _transform.GetMoverCoordinates(args.User);
        var barricade = SpawnAtPosition(ent.Comp.DeployedPrototype, coords);
        var barricadeXform = Transform(barricade);
        barricadeXform.LocalRotation = direction.ToAngle();

        if (!barricadeXform.Anchored)
            _transform.AnchorEntity((barricade, barricadeXform));

        ApplyStoredDamage(barricade, damage);

        _popup.PopupEntity(
            Loc.GetString("rmc-folding-barricade-deploy-finish", ("barricade", barricade)),
            barricade,
            args.User);
    }

    private void OnStackRepairDoAfter(Entity<RMCFoldingBarricadeStackComponent> ent, ref RMCFoldingBarricadeRepairDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        args.Handled = true;

        if (args.Used == null ||
            !_repairable.UseFuel(args.Used.Value, args.User, ent.Comp.RepairFuel))
        {
            return;
        }

        if (!_net.IsServer)
            return;

        NormalizeStoredDamage(ent);

        var repaired = false;
        for (var i = 0; i < ent.Comp.StoredDamage.Count; i++)
        {
            if (ent.Comp.StoredDamage[i] <= 0)
                continue;

            ent.Comp.StoredDamage[i] = MathHelper.Clamp(ent.Comp.StoredDamage[i] - ent.Comp.RepairAmount, 0, ent.Comp.MaxDamage);
            repaired = true;
        }

        if (!repaired)
            return;

        Dirty(ent);

        var selfMsg = Loc.GetString("rmc-repairable-finish-self", ("target", ent));
        var othersMsg = Loc.GetString("rmc-repairable-finish-others", ("user", args.User), ("target", ent));
        _popup.PopupPredicted(selfMsg, othersMsg, args.User, args.User);
        _audio.PlayPredicted(ent.Comp.RepairSound, ent, args.User);
    }

    private void OnStackExamined(Entity<RMCFoldingBarricadeStackComponent> ent, ref ExaminedEvent args)
    {
        using (args.PushGroup(nameof(RMCFoldingBarricadeStackComponent)))
        {
            args.PushMarkup(Loc.GetString("rmc-folding-barricade-stack-examine-deploy"));

            NormalizeStoredDamage(ent);
            if (IsStackDamaged(ent))
                args.PushMarkup(Loc.GetString("rmc-folding-barricade-stack-examine-damaged"));
        }
    }

    private void OnStackCountChanged(Entity<RMCFoldingBarricadeStackComponent> ent, ref StackCountChangedEvent args)
    {
        if (ent.Comp.SuppressCountChange)
            return;

        if (args.NewCount < args.OldCount)
        {
            var removed = args.OldCount - args.NewCount;
            NormalizeStoredDamage(ent, args.OldCount);

            for (var i = 0; i < removed; i++)
            {
                if (ent.Comp.StoredDamage.Count == 0)
                {
                    ent.Comp.PendingSplitDamage.Insert(0, 0);
                    continue;
                }

                var last = ent.Comp.StoredDamage[^1];
                ent.Comp.StoredDamage.RemoveAt(ent.Comp.StoredDamage.Count - 1);
                ent.Comp.PendingSplitDamage.Insert(0, last);
            }
        }
        else if (args.NewCount > args.OldCount)
        {
            var added = args.NewCount - args.OldCount;
            for (var i = 0; i < added; i++)
                ent.Comp.StoredDamage.Add(0);
        }

        NormalizeStoredDamage(ent, args.NewCount);
        Dirty(ent);
    }

    private void OnStackSplit(Entity<RMCFoldingBarricadeStackComponent> ent, ref StackSplitEvent args)
    {
        if (!TryComp(args.NewId, out RMCFoldingBarricadeStackComponent? splitFolding))
            return;

        var newCount = TryComp(args.NewId, out StackComponent? splitStack)
            ? splitStack.Count
            : 1;

        var damage = new List<float>(newCount);
        for (var i = 0; i < newCount; i++)
        {
            if (ent.Comp.PendingSplitDamage.Count > 0)
            {
                damage.Add(ClampDamage(ent.Comp.PendingSplitDamage[0], ent.Comp.MaxDamage));
                ent.Comp.PendingSplitDamage.RemoveAt(0);
                continue;
            }

            damage.Add(0);
        }

        splitFolding.StoredDamage = damage;
        NormalizeStoredDamage((args.NewId, splitFolding), splitStack);
        Dirty(args.NewId, splitFolding);

        NormalizeStoredDamage(ent);
        Dirty(ent);
    }

    private void OnBarricadeInteractUsing(Entity<RMCFoldingBarricadeComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (!_tool.HasQuality(args.Used, PryingQuality))
            return;

        args.Handled = true;

        if (!_skills.HasSkill(args.User, ent.Comp.CrowbarSkill, ent.Comp.CrowbarSkillRequired))
        {
            PopupClientPredicted(
                Loc.GetString("rmc-folding-barricade-collapse-crowbar-untrained", ("barricade", ent)),
                ent,
                args.User,
                PopupType.SmallCaution);
            return;
        }

        TryStartCollapse(ent, args.User, ent.Comp.CrowbarCollapseDelay, args.Used, true);
    }

    private void OnBarricadeGetAlternativeVerbs(Entity<RMCFoldingBarricadeComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        if (HasComp<XenoComponent>(args.User))
            return;

        var user = args.User;
        args.Verbs.Add(new AlternativeVerb
        {
            Text = Loc.GetString("rmc-folding-barricade-collapse-verb"),
            Act = () => TryStartCollapse(ent, user, ent.Comp.CollapseDelay),
            Priority = 1
        });
    }

    private void OnBarricadeCanDrag(Entity<RMCFoldingBarricadeComponent> ent, ref CanDragEvent args)
    {
        args.Handled = true;
    }

    private void OnBarricadeCanDropDragged(Entity<RMCFoldingBarricadeComponent> ent, ref CanDropDraggedEvent args)
    {
        if (args.User != args.Target)
            return;

        if (HasComp<XenoComponent>(args.User))
            return;

        args.CanDrop = true;
        args.Handled = true;
    }

    private void OnBarricadeDragDropDragged(Entity<RMCFoldingBarricadeComponent> ent, ref DragDropDraggedEvent args)
    {
        if (args.User != args.Target)
            return;

        if (HasComp<XenoComponent>(args.User))
            return;

        args.Handled = true;
        TryStartCollapse(ent, args.User, ent.Comp.CollapseDelay);
    }

    private void OnBarricadeCollapseDoAfter(Entity<RMCFoldingBarricadeComponent> ent, ref RMCFoldingBarricadeCollapseDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        args.Handled = true;

        if (!_net.IsServer)
            return;

        var damage = 0f;
        if (TryComp(ent, out DamageableComponent? damageable))
            damage = damageable.TotalDamage.Float();

        damage = ClampDamage(damage, ent.Comp.MaxDamage);

        if (!TryReturnFolded(ent, args.User, damage))
            return;

        _popup.PopupEntity(
            Loc.GetString("rmc-folding-barricade-collapse-finish", ("barricade", ent)),
            args.User,
            args.User);
        QueueDel(ent);
    }

    private void OnBarricadeExamined(Entity<RMCFoldingBarricadeComponent> ent, ref ExaminedEvent args)
    {
        if (HasComp<XenoComponent>(args.Examiner))
            return;

        using (args.PushGroup(nameof(RMCFoldingBarricadeComponent)))
        {
            args.PushMarkup(Loc.GetString("rmc-folding-barricade-deployed-examine"));
        }
    }

    private bool TryStartDeploy(Entity<RMCFoldingBarricadeStackComponent> ent, EntityUid user)
    {
        if (!CanDeploy(ent, user, out _, out var popup))
        {
            if (popup != null)
                PopupEntityPredicted(popup, ent, user);

            return false;
        }

        var ev = new RMCFoldingBarricadeDeployDoAfterEvent();
        var doAfter = new DoAfterArgs(EntityManager, user, ent.Comp.DeployDelay, ev, ent, ent)
        {
            NeedHand = true,
            BreakOnMove = true,
            BreakOnDamage = true,
            BreakOnHandChange = true,
            BlockDuplicate = true,
            DuplicateCondition = DuplicateConditions.SameEvent
        };

        if (!_doAfter.TryStartDoAfter(doAfter))
            return false;

        _popup.PopupPredicted(
            Loc.GetString("rmc-folding-barricade-deploy-start", ("barricade", ent)),
            Loc.GetString("rmc-folding-barricade-deploy-start-others", ("user", user), ("barricade", ent)),
            user,
            user);
        return true;
    }

    private bool CanDeploy(Entity<RMCFoldingBarricadeStackComponent> ent, EntityUid user, out Direction direction, out string? popup)
    {
        direction = Direction.Invalid;
        popup = null;

        if (!_construction.CanConstruct(user))
        {
            popup = Loc.GetString("rmc-construction-structure-blocked");
            return false;
        }

        if (!TryComp(ent, out StackComponent? stack) || stack.Count <= 0)
            return false;

        if (HasAcid(ent))
        {
            popup = Loc.GetString("rmc-acid-pickup-blocked", ("target", ent));
            return false;
        }

        var userXform = Transform(user);
        direction = userXform.LocalRotation.GetCardinalDir();
        var coords = userXform.Coordinates;

        return _construction.CanBuildAt(
            coords,
            ent.Comp.DeployedPrototype,
            out popup,
            direction: direction,
            collision: CollisionGroup.Impassable,
            user: user);
    }

    private void TryStartStackRepair(Entity<RMCFoldingBarricadeStackComponent> ent, EntityUid user, EntityUid used)
    {
        if (!HasComp<BlowtorchComponent>(used))
        {
            PopupClientPredicted(Loc.GetString("rmc-repairable-need-blowtorch"), user, user);
            return;
        }

        NormalizeStoredDamage(ent);
        var damaged = GetDamagedCount(ent);
        if (damaged == 0)
        {
            PopupClientPredicted(
                Loc.GetString("rmc-repairable-not-damaged", ("target", ent)),
                user,
                user,
                PopupType.SmallCaution);
            return;
        }

        if (!_repairable.UseFuel(used, user, ent.Comp.RepairFuel, true))
            return;

        var delay = TimeSpan.FromSeconds(ent.Comp.RepairDelayPerDamaged * damaged * _skills.GetSkillDelayMultiplier(user, ent.Comp.RepairSkill));
        var ev = new RMCFoldingBarricadeRepairDoAfterEvent();
        var doAfter = new DoAfterArgs(EntityManager, user, delay, ev, ent, ent, used: used)
        {
            NeedHand = true,
            BreakOnMove = true,
            BreakOnDamage = true,
            BreakOnHandChange = true,
            BlockDuplicate = true,
            DuplicateCondition = DuplicateConditions.SameEvent
        };

        if (!_doAfter.TryStartDoAfter(doAfter))
            return;

        _popup.PopupPredicted(
            Loc.GetString("rmc-repairable-start-self", ("target", ent)),
            Loc.GetString("rmc-repairable-start-others", ("user", user), ("target", ent)),
            user,
            user);
    }

    private bool TryStartCollapse(
        Entity<RMCFoldingBarricadeComponent> ent,
        EntityUid user,
        TimeSpan delay,
        EntityUid? used = null,
        bool needHand = false)
    {
        if (!_actionBlocker.CanInteract(user, ent))
            return false;

        var ev = new RMCFoldingBarricadeCollapseDoAfterEvent();
        var doAfter = new DoAfterArgs(EntityManager, user, delay, ev, ent, ent, used: used)
        {
            NeedHand = needHand,
            BreakOnMove = true,
            BreakOnDamage = true,
            BreakOnHandChange = used != null,
            BlockDuplicate = true,
            DuplicateCondition = DuplicateConditions.SameTarget
        };

        if (!_doAfter.TryStartDoAfter(doAfter))
            return false;

        _popup.PopupPredicted(
            Loc.GetString("rmc-folding-barricade-collapse-start", ("barricade", ent)),
            Loc.GetString("rmc-folding-barricade-collapse-start-others", ("user", user), ("barricade", ent)),
            user,
            user);
        return true;
    }

    private bool TryReturnFolded(Entity<RMCFoldingBarricadeComponent> ent, EntityUid user, float damage)
    {
        if (TryComp(user, out HandsComponent? hands))
        {
            foreach (var held in _hands.EnumerateHeld((user, hands)))
            {
                if (!TryComp(held, out RMCFoldingBarricadeStackComponent? heldFolding))
                    continue;

                if (!TryComp(held, out StackComponent? heldStack))
                    continue;

                if (AddStoredDamageToStack((held, heldFolding), damage, heldStack))
                    return true;
            }
        }

        var folded = SpawnAtPosition(ent.Comp.FoldedPrototype, Transform(ent).Coordinates);
        if (TryComp(folded, out RMCFoldingBarricadeStackComponent? folding) &&
            TryComp(folded, out StackComponent? stack))
        {
            folding.StoredDamage.Clear();
            folding.StoredDamage.Add(ClampDamage(damage, folding.MaxDamage));
            SetCountPreserving((folded, folding), 1, stack);
            Dirty(folded, folding);
        }

        _hands.TryPickupAnyHand(user, folded);
        return true;
    }

    private bool TryMergeStacks(
        Entity<RMCFoldingBarricadeStackComponent> donor,
        Entity<RMCFoldingBarricadeStackComponent> recipient,
        EntityUid user,
        EntityCoordinates popupLocation)
    {
        if (donor.Owner == recipient.Owner)
            return false;

        if (!TryComp(donor, out StackComponent? donorStack) ||
            !TryComp(recipient, out StackComponent? recipientStack))
        {
            return false;
        }

        if (donorStack.StackTypeId != recipientStack.StackTypeId)
            return false;

        if (HasAcid(donor) || HasAcid(recipient))
        {
            PopupCoordinatesPredicted(Loc.GetString("rmc-acid-pickup-blocked", ("target", donor)), popupLocation);
            return false;
        }

        NormalizeStoredDamage(donor, donorStack);
        NormalizeStoredDamage(recipient, recipientStack);

        var available = _stack.GetAvailableSpace(recipientStack);
        var transfer = Math.Min(donorStack.Count, available);
        if (transfer <= 0)
        {
            PopupCoordinatesPredicted(Loc.GetString("comp-stack-already-full"), popupLocation);
            return false;
        }

        for (var i = 0; i < transfer; i++)
        {
            var damage = 0f;
            if (donor.Comp.StoredDamage.Count > 0)
            {
                damage = donor.Comp.StoredDamage[^1];
                donor.Comp.StoredDamage.RemoveAt(donor.Comp.StoredDamage.Count - 1);
            }

            recipient.Comp.StoredDamage.Add(damage);
        }

        SetCountPreserving(donor, donorStack.Count - transfer, donorStack);
        SetCountPreserving(recipient, recipientStack.Count + transfer, recipientStack);
        NormalizeStoredDamage(donor, donorStack);
        NormalizeStoredDamage(recipient, recipientStack);
        Dirty(donor);
        Dirty(recipient);

        PopupCoordinatesPredicted($"+{transfer}", popupLocation);
        return true;
    }

    private void PopupEntityPredicted(
        string message,
        EntityUid target,
        EntityUid recipient,
        PopupType type = PopupType.SmallCaution)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        _popup.PopupEntity(message, target, recipient, type);
    }

    private void PopupClientPredicted(
        string message,
        EntityUid target,
        EntityUid recipient,
        PopupType type = PopupType.SmallCaution)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        _popup.PopupClient(message, target, recipient, type);
    }

    private void PopupCoordinatesPredicted(
        string message,
        EntityCoordinates coordinates,
        PopupType type = PopupType.Small)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        _popup.PopupCoordinates(message, coordinates, type);
    }

    private bool AddStoredDamageToStack(Entity<RMCFoldingBarricadeStackComponent> ent, float damage, StackComponent? stack = null)
    {
        if (!Resolve(ent, ref stack, false))
            return false;

        NormalizeStoredDamage(ent, stack);

        if (_stack.GetAvailableSpace(stack) <= 0)
            return false;

        ent.Comp.StoredDamage.Add(ClampDamage(damage, ent.Comp.MaxDamage));
        SetCountPreserving(ent, stack.Count + 1, stack);
        NormalizeStoredDamage(ent, stack);
        Dirty(ent);
        return true;
    }

    private float PopStoredDamage(Entity<RMCFoldingBarricadeStackComponent> ent, StackComponent? stack = null, bool changeCount = false)
    {
        NormalizeStoredDamage(ent, stack);

        var damage = 0f;
        if (ent.Comp.StoredDamage.Count > 0)
        {
            damage = ent.Comp.StoredDamage[^1];
            ent.Comp.StoredDamage.RemoveAt(ent.Comp.StoredDamage.Count - 1);
        }

        if (changeCount && Resolve(ent, ref stack, false))
            SetCountPreserving(ent, stack.Count - 1, stack);

        Dirty(ent);
        return ClampDamage(damage, ent.Comp.MaxDamage);
    }

    private void NormalizeStoredDamage(Entity<RMCFoldingBarricadeStackComponent> ent)
    {
        if (TryComp(ent, out StackComponent? stack))
            NormalizeStoredDamage(ent, stack);
        else
            NormalizeStoredDamage(ent, ent.Comp.StoredDamage.Count);
    }

    private void NormalizeStoredDamage(Entity<RMCFoldingBarricadeStackComponent> ent, StackComponent? stack)
    {
        NormalizeStoredDamage(ent, stack?.Count ?? ent.Comp.StoredDamage.Count);
    }

    private void NormalizeStoredDamage(Entity<RMCFoldingBarricadeStackComponent> ent, int count)
    {
        count = Math.Max(0, count);

        for (var i = 0; i < ent.Comp.StoredDamage.Count; i++)
            ent.Comp.StoredDamage[i] = ClampDamage(ent.Comp.StoredDamage[i], ent.Comp.MaxDamage);

        while (ent.Comp.StoredDamage.Count < count)
            ent.Comp.StoredDamage.Add(0);

        while (ent.Comp.StoredDamage.Count > count)
            ent.Comp.StoredDamage.RemoveAt(ent.Comp.StoredDamage.Count - 1);

        Dirty(ent);
    }

    private void SetCountPreserving(Entity<RMCFoldingBarricadeStackComponent> ent, int count, StackComponent stack)
    {
        ent.Comp.SuppressCountChange = true;
        try
        {
            _stack.SetCount(ent, count, stack);
        }
        finally
        {
            ent.Comp.SuppressCountChange = false;
        }
    }

    private void ApplyStoredDamage(EntityUid target, float damage)
    {
        damage = ClampDamage(damage, float.MaxValue);
        if (damage <= 0 || !TryComp(target, out DamageableComponent? damageable))
            return;

        var spec = _rmcDamageable.DistributeTypesTotal((target, damageable), FixedPoint2.New(damage));
        _damageable.TryChangeDamage(target, spec, true, false, damageable);
    }

    private bool HasAcid(EntityUid uid)
    {
        return HasComp<TimedCorrodingComponent>(uid) ||
               HasComp<DamageableCorrodingComponent>(uid) ||
               HasComp<SprayAcidedComponent>(uid);
    }

    private bool IsStackDamaged(Entity<RMCFoldingBarricadeStackComponent> ent)
    {
        var threshold = ent.Comp.MaxDamage * 0.25f;
        foreach (var damage in ent.Comp.StoredDamage)
        {
            if (damage >= threshold)
                return true;
        }

        return false;
    }

    private int GetDamagedCount(Entity<RMCFoldingBarricadeStackComponent> ent)
    {
        var count = 0;
        foreach (var damage in ent.Comp.StoredDamage)
        {
            if (damage > 0)
                count++;
        }

        return count;
    }

    private static float ClampDamage(float damage, float maxDamage)
    {
        if (float.IsNaN(damage))
            return 0;

        return MathHelper.Clamp(damage, 0, maxDamage);
    }
}
