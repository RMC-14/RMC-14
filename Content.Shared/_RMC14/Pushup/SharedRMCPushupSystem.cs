using System.Linq;
using Content.Shared._RMC14.Dialog;
using Content.Shared._RMC14.DoAfter;
using Content.Shared._RMC14.Input;
using Content.Shared._RMC14.Marines.Skills;
using Content.Shared._RMC14.Stamina;
using Content.Shared._RMC14.Standing;
using Content.Shared._RMC14.Synth;
using Content.Shared.ActionBlocker;
using Content.Shared.Body.Components;
using Content.Shared.Body.Part;
using Content.Shared.Body.Systems;
using Content.Shared.Buckle.Components;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.Inventory;
using Content.Shared.Item;
using Content.Shared.Mobs.Systems;
using Content.Shared.Nutrition;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Standing;
using Robust.Shared.Containers;
using Robust.Shared.Input.Binding;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Pushup;

public sealed class SharedRMCPushupSystem : EntitySystem
{
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly SharedBodySystem _body = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly DialogSystem _dialog = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly HungerSystem _hunger = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedItemSystem _item = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly MobThresholdSystem _mobThreshold = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly RMCDoAfterSystem _rmcDoAfter = default!;
    [Dependency] private readonly SkillsSystem _skills = default!;
    [Dependency] private readonly RMCStaminaSystem _stamina = default!;
    [Dependency] private readonly StandingStateSystem _standing = default!;

    private static readonly EntProtoId<SkillDefinitionComponent> EnduranceSkill = "RMCSkillEndurance";

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCPushupComponent, DoAfterAttemptEvent<RMCPushupDoAfterEvent>>(OnDoAfterAttempt);
        SubscribeLocalEvent<RMCPushupComponent, RMCPushupDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<RMCPushupComponent, RMCPushupSelectedEvent>(OnSelected);
        SubscribeNetworkEvent<RMCPushupSelectedEvent>(OnSelectedNetwork);

        CommandBinds.Builder
            .Bind(CMKeyFunctions.RMCPushup,
                InputCmdHandler.FromDelegate(session =>
                {
                    if (session?.AttachedEntity is { } user)
                        TryStart(user, RMCPushupForm.Proper, false);
                }, handle: false))
            .Bind(CMKeyFunctions.RMCWeakPushup,
                InputCmdHandler.FromDelegate(session =>
                {
                    if (session?.AttachedEntity is { } user)
                        TryStart(user, RMCPushupForm.Knees, false);
                }, handle: false))
            .Bind(CMKeyFunctions.RMCPushupRoutine,
                InputCmdHandler.FromDelegate(session =>
                {
                    if (session?.AttachedEntity is { } user)
                        OpenRoutineDialog(user);
                }, handle: false))
            .Register<SharedRMCPushupSystem>();
    }

    public override void Shutdown()
    {
        base.Shutdown();
        CommandBinds.Unregister<SharedRMCPushupSystem>();
    }

    private void OnSelected(Entity<RMCPushupComponent> ent, ref RMCPushupSelectedEvent args)
    {
        TryStart(ent.Owner, args.Form, true);
    }

    private void OnSelectedNetwork(RMCPushupSelectedEvent args, EntitySessionEventArgs session)
    {
        if (!_net.IsServer || session.SenderSession.AttachedEntity is not { } user)
            return;

        TryStart(user, args.Form, true);
    }

    private void OnDoAfterAttempt(Entity<RMCPushupComponent> ent,
        ref DoAfterAttemptEvent<RMCPushupDoAfterEvent> args)
    {
        if (!CanPushup(ent.Owner, false))
            args.Cancel();
    }

    private void OnDoAfter(Entity<RMCPushupComponent> ent, ref RMCPushupDoAfterEvent args)
    {
        if (args.Handled)
            return;

        if (args.Cancelled)
        {
            Stop(ent, true);
            return;
        }

        if (!CanPushup(ent.Owner, false) || !TryComp(ent, out RMCStaminaComponent? stamina))
        {
            Stop(ent, true);
            return;
        }

        args.Handled = true;
        ent.Comp.Count++;

        var cost = CalculateStaminaCost(ent.Owner);
        var minimum = stamina.Max * ent.Comp.MinimumStaminaFraction;
        var available = Math.Max(0, stamina.Current - minimum);
        var applied = Math.Min(cost, available);
        if (applied > 0)
            _stamina.DoStaminaDamage((ent, stamina), applied);

        PopupCompleted(ent);

        if (!ent.Comp.Routine)
        {
            Stop(ent, false);
            return;
        }

        if (stamina.Current <= minimum)
        {
            _popup.PopupPredicted(
                Loc.GetString("rmc-pushup-too-tired"),
                ent,
                ent,
                PopupType.MediumCaution);
            Stop(ent, false);
            return;
        }

        args.Repeat = true;
    }

    public void OpenRoutineDialog(Entity<RMCPushupComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        var pushup = ent.Comp!;

        if (pushup.Active)
        {
            Cancel(ent.Owner);
            return;
        }

        if (!CanPushup(ent.Owner, true))
            return;

        var options = new List<DialogOption>
        {
            new(Loc.GetString("rmc-pushup-form-proper"), new RMCPushupSelectedEvent(RMCPushupForm.Proper)),
            new(Loc.GetString("rmc-pushup-form-knees"), new RMCPushupSelectedEvent(RMCPushupForm.Knees)),
        };

        _dialog.OpenOptions(
            ent,
            Loc.GetString("rmc-pushup-dialog-title"),
            options,
            Loc.GetString("rmc-pushup-dialog-prompt"));
    }

    public bool TryStart(Entity<RMCPushupComponent?> ent, RMCPushupForm form, bool routine)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return false;

        var pushup = ent.Comp!;
        Entity<RMCPushupComponent> resolved = (ent.Owner, pushup);

        if (pushup.Active)
        {
            Cancel(ent.Owner);
            return false;
        }

        if (!CanPushup(ent.Owner, true))
            return false;

        var doAfter = new DoAfterArgs(
            EntityManager,
            ent,
            pushup.Duration,
            new RMCPushupDoAfterEvent(),
            ent)
        {
            AttemptFrequency = AttemptFrequency.EveryTick,
            BreakOnDamage = true,
            BreakOnMove = true,
            BreakOnRest = false,
            DuplicateCondition = DuplicateConditions.SameEvent,
            RequireCanInteract = true,
        };

        if (!_doAfter.TryStartDoAfter(doAfter, out var doAfterId))
            return false;

        pushup.Active = true;
        pushup.Form = form;
        pushup.Routine = routine;
        pushup.Count = 0;
        pushup.CurrentDoAfter = doAfterId.Value.Index;
        Dirty(resolved);
        RaiseVisualsChanged(resolved);

        if (routine)
            PopupStarted(resolved);

        return true;
    }

    public void Cancel(Entity<RMCPushupComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        var pushup = ent.Comp!;
        Entity<RMCPushupComponent> resolved = (ent.Owner, pushup);
        if (!pushup.Active)
            return;

        _rmcDoAfter.TryCancel((ent.Owner, CompOrNull<DoAfterComponent>(ent.Owner)), pushup.CurrentDoAfter);
        Stop(resolved, true);
    }

    public bool CanPushup(Entity<RMCPushupComponent?> ent, bool doPopup = false)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return false;

        var pushup = ent.Comp!;
        string? popup = null;

        if (!_actionBlocker.CanInteract(ent, null) || _mobState.IsIncapacitated(ent))
            popup = "rmc-pushup-cant-interact";
        else if (!TryComp(ent, out RMCRestComponent? rest) || !rest.Resting || !_standing.IsDown(ent))
            popup = "rmc-pushup-must-rest";
        else if (TryComp(ent, out BuckleComponent? buckle) && buckle.Buckled)
            popup = "rmc-pushup-buckled";
        else if (_container.IsEntityInContainer(ent))
            popup = "rmc-pushup-invalid-location";
        else if (!HasRequiredLimbs(ent))
            popup = "rmc-pushup-missing-limbs";
        else if (!TryComp(ent, out RMCStaminaComponent? stamina) ||
                 stamina.Current <= stamina.Max * pushup.MinimumStaminaFraction)
            popup = "rmc-pushup-too-weak";

        if (popup == null)
            return true;

        if (doPopup)
            _popup.PopupPredicted(Loc.GetString(popup), ent, ent, PopupType.SmallCaution);

        return false;
    }

    public double CalculateStaminaCost(Entity<RMCPushupComponent?> ent, RMCPushupForm? form = null)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return 0;

        var pushup = ent.Comp!;
        if (HasComp<SynthComponent>(ent))
            return 0;

        var cost = pushup.BaseStaminaCost;
        cost += _skills.GetSkill((ent.Owner, CompOrNull<SkillsComponent>(ent.Owner)), EnduranceSkill) switch
        {
            <= 0 => pushup.NoEnduranceModifier,
            2 => pushup.TrainedEnduranceModifier,
            3 => pushup.MasterEnduranceModifier,
            >= 4 => pushup.ExpertEnduranceModifier,
            _ => 0,
        };

        var slots = _inventory.GetSlotEnumerator(
            (ent.Owner, CompOrNull<InventoryComponent>(ent.Owner)),
            SlotFlags.OUTERCLOTHING | SlotFlags.BACK);
        while (slots.NextItem(out var itemUid))
        {
            if (!TryComp(itemUid, out ItemComponent? item))
                continue;

            var weight = Math.Max(1, _item.GetItemSizeWeight(item.Size));
            var itemClass = Math.Clamp(1 + (int) Math.Floor(Math.Log2(weight)), 1, 5);
            cost += itemClass * pushup.GearClassModifier;
        }

        if (TryComp(ent, out HungerComponent? hunger) &&
            _hunger.GetHungerThreshold(hunger) <= HungerThreshold.Starving)
        {
            cost += pushup.StarvingModifier;
        }

        if (TryComp(ent, out DamageableComponent? damageable) &&
            _mobThreshold.TryGetIncapPercentage(ent, damageable.TotalDamage, out var damagePercentage) &&
            damagePercentage >= FixedPoint2.New(pushup.InjuredThreshold))
        {
            cost += pushup.InjuredModifier;
        }

        if ((form ?? pushup.Form) == RMCPushupForm.Knees)
            cost += pushup.KneeModifier;

        return Math.Max(pushup.MinimumStaminaCost, cost);
    }

    private bool HasRequiredLimbs(EntityUid ent)
    {
        if (!HasComp<BodyComponent>(ent))
            return false;

        return HasPart(ent, BodyPartType.Arm, BodyPartSymmetry.Left) &&
               HasPart(ent, BodyPartType.Arm, BodyPartSymmetry.Right) &&
               HasPart(ent, BodyPartType.Hand, BodyPartSymmetry.Left) &&
               HasPart(ent, BodyPartType.Hand, BodyPartSymmetry.Right) &&
               HasPart(ent, BodyPartType.Leg, BodyPartSymmetry.Left) &&
               HasPart(ent, BodyPartType.Leg, BodyPartSymmetry.Right) &&
               HasPart(ent, BodyPartType.Foot, BodyPartSymmetry.Left) &&
               HasPart(ent, BodyPartType.Foot, BodyPartSymmetry.Right);
    }

    private bool HasPart(EntityUid ent, BodyPartType type, BodyPartSymmetry symmetry)
    {
        return _body.GetBodyChildrenOfType(ent, type).Any(part => part.Component.Symmetry == symmetry);
    }

    private void Stop(Entity<RMCPushupComponent> ent, bool cancelled)
    {
        if (!ent.Comp.Active)
            return;

        var routine = ent.Comp.Routine;
        ent.Comp.Active = false;
        ent.Comp.Routine = false;
        ent.Comp.CurrentDoAfter = null;
        Dirty(ent);
        RaiseVisualsChanged(ent);

        if (cancelled && routine)
        {
            _popup.PopupPredicted(
                Loc.GetString("rmc-pushup-stop-self"),
                Loc.GetString("rmc-pushup-stop-others", ("user", ent.Owner)),
                ent,
                ent);
        }
    }

    private void PopupStarted(Entity<RMCPushupComponent> ent)
    {
        var suffix = ent.Comp.Form == RMCPushupForm.Knees ? "knees" : "proper";
        _popup.PopupPredicted(
            Loc.GetString($"rmc-pushup-start-{suffix}-self"),
            Loc.GetString($"rmc-pushup-start-{suffix}-others", ("user", ent.Owner)),
            ent,
            ent);
    }

    private void PopupCompleted(Entity<RMCPushupComponent> ent)
    {
        var suffix = ent.Comp.Form == RMCPushupForm.Knees ? "knees" : "proper";
        if (ent.Comp.Routine)
        {
            _popup.PopupPredicted(
                Loc.GetString($"rmc-pushup-routine-{suffix}-self", ("count", ent.Comp.Count)),
                Loc.GetString($"rmc-pushup-routine-{suffix}-others", ("user", ent.Owner), ("count", ent.Comp.Count)),
                ent,
                ent,
                PopupType.Medium);
            return;
        }

        _popup.PopupPredicted(
            Loc.GetString($"rmc-pushup-single-{suffix}-self"),
            Loc.GetString($"rmc-pushup-single-{suffix}-others", ("user", ent.Owner)),
            ent,
            ent,
            PopupType.Medium);
    }

    private void RaiseVisualsChanged(Entity<RMCPushupComponent> ent)
    {
        var ev = new RMCPushupVisualsChangedEvent();
        RaiseLocalEvent(ent, ref ev);
    }
}
