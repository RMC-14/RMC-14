using Content.Shared._RMC14.Tools;
using Content.Shared._RMC14.Xenonids.Acid;
using Content.Shared._RMC14.Xenonids.Energy;
using Content.Shared._RMC14.Xenonids.Plasma;
using Content.Shared.DoAfter;
using Content.Shared.Explosion.Components;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Sticky.Components;
using Content.Shared.Sticky.Systems;
using Content.Shared.Trigger;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Explosion;

public sealed class RMCC4DisarmableSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly StickySystem _sticky = default!;
    [Dependency] private readonly XenoEnergySystem _xenoEnergy = default!;
    [Dependency] private readonly XenoPlasmaSystem _xenoPlasma = default!;

    private const string StickerSlotId = "stickers_container";

    public override void Initialize()
    {
        SubscribeLocalEvent<XenoAcidComponent, BeforeXenoCorrosiveAcidEvent>(OnXenoAcid);
        SubscribeLocalEvent<XenoAcidComponent, DoAfterAttemptEvent<RMCC4AcidDoAfterEvent>>(OnC4AcidDoAfterAttempt);
        SubscribeLocalEvent<XenoAcidComponent, RMCC4AcidDoAfterEvent>(OnC4AcidDoAfter);

        SubscribeLocalEvent<RMCC4DisarmableComponent, InteractUsingEvent>(OnC4InteractUsing);
        SubscribeLocalEvent<RMCC4DisarmableComponent, RMCC4MultitoolDisarmDoAfterEvent>(OnMultitoolDoAfter);
        SubscribeLocalEvent<MobStateComponent, AccessibleOverrideEvent>(OnMobAccessibleOverride);
        SubscribeLocalEvent<MobStateComponent, InRangeOverrideEvent>(OnMobInRangeOverride);
        SubscribeLocalEvent<MobStateComponent, InteractUsingEvent>(OnMobInteractUsing);
        SubscribeLocalEvent<RMCWallExplosionDeletableComponent, InteractUsingEvent>(OnWallInteractUsing);
    }

    private void OnXenoAcid(Entity<XenoAcidComponent> xeno, ref BeforeXenoCorrosiveAcidEvent args)
    {
        if (args.Handled)
            return;

        if (!TryGetC4(args.Target, out var c4))
            return;

        var acid = args.Acid;
        if (acid.Strength < c4.Comp.MinimumAcidStrength)
        {
            args.Handled = true;
            _popup.PopupClient(
                Loc.GetString(c4.Comp.AcidTooWeakPopup, ("explosive", c4.Owner)),
                args.Target,
                xeno,
                PopupType.SmallCaution);
            return;
        }

        args.Handled = true;

        var delay = c4.Comp.AcidDelay * acid.ApplyTimeMultiplier;
        var doAfter = new DoAfterArgs(EntityManager,
            xeno,
            delay,
            new RMCC4AcidDoAfterEvent(acid, GetNetEntity(c4.Owner)),
            xeno,
            args.Target)
        {
            BreakOnMove = true,
            RequireCanInteract = false,
            AttemptFrequency = AttemptFrequency.StartAndEnd,
        };

        if (_doAfter.TryStartDoAfter(doAfter))
        {
            _popup.PopupClient(
                Loc.GetString(c4.Comp.AcidStartPopup, ("explosive", c4.Owner)),
                args.Target,
                xeno);
        }
    }

    private void OnC4AcidDoAfterAttempt(Entity<XenoAcidComponent> xeno, ref DoAfterAttemptEvent<RMCC4AcidDoAfterEvent> args)
    {
        if (args.Cancelled)
            return;

        if (_mobState.IsIncapacitated(xeno))
            args.Cancel();
    }

    private void OnC4AcidDoAfter(Entity<XenoAcidComponent> xeno, ref RMCC4AcidDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Target is not { } target)
            return;

        if (!TryGetEntity(args.C4, out var c4Id) ||
            TerminatingOrDeleted(c4Id.Value) ||
            !TryComp(c4Id.Value, out RMCC4DisarmableComponent? disarmable) ||
            !IsC4OnTarget(target, c4Id.Value) ||
            disarmable.MinimumAcidStrength > args.Strength)
        {
            return;
        }

        Entity<RMCC4DisarmableComponent> c4 = (c4Id.Value, disarmable);

        if (args.PlasmaCost != FixedPoint2.Zero && !_xenoPlasma.TryRemovePlasmaPopup(xeno.Owner, args.PlasmaCost))
            return;

        if (args.EnergyCost != 0 && !_xenoEnergy.TryRemoveEnergyPopup(xeno.Owner, args.EnergyCost))
            return;

        args.Handled = true;

        if (_net.IsClient)
            return;

        _popup.PopupEntity(
            Loc.GetString(c4.Comp.AcidFinishPopup, ("explosive", c4.Owner)),
            target,
            xeno);

        StopTimer(c4.Owner);
        QueueDel(c4.Owner);
    }

    private void OnC4InteractUsing(Entity<RMCC4DisarmableComponent> c4, ref InteractUsingEvent args)
    {
        TryStartMultitoolDisarm(c4, args.User, args.Used, GetDisarmTarget(c4.Owner), ref args);
    }

    private void OnMobAccessibleOverride(Entity<MobStateComponent> user, ref AccessibleOverrideEvent args)
    {
        if (args.Handled || !TryGetStickySurface(args.Target, out var surface))
            return;

        args.Handled = true;
        args.Accessible = _interaction.IsAccessible((user.Owner, null), (surface, null));
    }

    private void OnMobInRangeOverride(Entity<MobStateComponent> user, ref InRangeOverrideEvent args)
    {
        if (args.Handled || !TryGetStickySurface(args.Target, out var surface))
            return;

        args.Handled = true;
        args.InRange = _interaction.InRangeUnobstructed(user.Owner, surface);
    }

    private void OnMobInteractUsing(Entity<MobStateComponent> target, ref InteractUsingEvent args)
    {
        TryStartContainedMultitoolDisarm(target.Owner, ref args);
    }

    private void OnWallInteractUsing(Entity<RMCWallExplosionDeletableComponent> target, ref InteractUsingEvent args)
    {
        TryStartContainedMultitoolDisarm(target.Owner, ref args);
    }

    private void TryStartContainedMultitoolDisarm(EntityUid target, ref InteractUsingEvent args)
    {
        if (TryGetContainedC4(target, out var c4))
            TryStartMultitoolDisarm(c4, args.User, args.Used, target, ref args);
    }

    private void TryStartMultitoolDisarm(
        Entity<RMCC4DisarmableComponent> c4,
        EntityUid user,
        EntityUid used,
        EntityUid target,
        ref InteractUsingEvent args)
    {
        if (args.Handled || !HasComp<MultitoolComponent>(used))
            return;

        if (!HasComp<ActiveTimerTriggerComponent>(c4.Owner))
            return;

        if (!IsC4OnTarget(target, c4.Owner))
            return;

        args.Handled = true;

        var doAfter = new DoAfterArgs(EntityManager,
            user,
            c4.Comp.MultitoolDelay,
            new RMCC4MultitoolDisarmDoAfterEvent(),
            c4.Owner,
            target,
            used)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true,
            AttemptFrequency = AttemptFrequency.StartAndEnd,
        };

        if (_doAfter.TryStartDoAfter(doAfter))
        {
            _popup.PopupClient(
                Loc.GetString(c4.Comp.MultitoolStartPopup, ("explosive", c4.Owner)),
                target,
                user);
        }
    }

    private void OnMultitoolDoAfter(Entity<RMCC4DisarmableComponent> c4, ref RMCC4MultitoolDisarmDoAfterEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        if (args.Cancelled)
        {
            _popup.PopupClient(
                Loc.GetString(c4.Comp.MultitoolStopPopup, ("explosive", c4.Owner)),
                args.Target ?? c4.Owner,
                args.User);
            return;
        }

        if (!HasComp<ActiveTimerTriggerComponent>(c4.Owner))
            return;

        if (args.Target is { } target && !IsC4OnTarget(target, c4.Owner))
            return;

        StopTimer(c4.Owner);

        if (TryComp(c4.Owner, out StickyComponent? sticky))
            _sticky.UnstickFromEntity((c4.Owner, sticky), args.User);

        _popup.PopupClient(
            Loc.GetString(c4.Comp.MultitoolFinishPopup, ("explosive", c4.Owner)),
            args.Target ?? c4.Owner,
            args.User);
    }

    private EntityUid GetDisarmTarget(EntityUid c4)
    {
        return TryGetStickySurface(c4, out var surface) ? surface : c4;
    }

    private bool TryGetStickySurface(EntityUid c4, out EntityUid surface)
    {
        surface = default;

        if (!HasComp<RMCC4DisarmableComponent>(c4) ||
            !TryComp(c4, out StickyComponent? sticky) ||
            sticky.StuckTo is not { } stuckTo ||
            TerminatingOrDeleted(stuckTo))
        {
            return false;
        }

        surface = stuckTo;
        return true;
    }

    private bool TryGetC4(EntityUid target, out Entity<RMCC4DisarmableComponent> c4)
    {
        if (TryComp(target, out RMCC4DisarmableComponent? direct))
        {
            c4 = (target, direct);
            return true;
        }

        return TryGetContainedC4(target, out c4);
    }

    private bool TryGetContainedC4(EntityUid target, out Entity<RMCC4DisarmableComponent> c4)
    {
        if (!_container.TryGetContainer(target, StickerSlotId, out var container))
        {
            c4 = default;
            return false;
        }

        foreach (var contained in container.ContainedEntities)
        {
            if (!TryComp(contained, out RMCC4DisarmableComponent? disarmable))
                continue;

            c4 = (contained, disarmable);
            return true;
        }

        c4 = default;
        return false;
    }

    private bool IsC4OnTarget(EntityUid target, EntityUid c4)
    {
        if (target == c4)
            return true;

        if (!_container.TryGetContainer(target, StickerSlotId, out var container))
            return false;

        foreach (var contained in container.ContainedEntities)
        {
            if (contained == c4)
                return true;
        }

        return false;
    }

    private void StopTimer(EntityUid uid)
    {
        RemComp<ActiveTimerTriggerComponent>(uid);

        if (TryComp(uid, out AppearanceComponent? appearance))
            _appearance.SetData(uid, TriggerVisuals.VisualState, TriggerVisualState.Unprimed, appearance);
    }
}

[Serializable, NetSerializable]
public sealed partial class RMCC4AcidDoAfterEvent : DoAfterEvent
{
    [DataField]
    public NetEntity C4;

    [DataField]
    public XenoAcidStrength Strength = XenoAcidStrength.Normal;

    [DataField]
    public FixedPoint2 PlasmaCost = 100;

    [DataField]
    public int EnergyCost;

    public RMCC4AcidDoAfterEvent(XenoCorrosiveAcidEvent ev, NetEntity c4)
    {
        C4 = c4;
        Strength = ev.Strength;
        PlasmaCost = ev.PlasmaCost;
        EnergyCost = ev.EnergyCost;
    }

    public override DoAfterEvent Clone()
    {
        return this;
    }
}

[Serializable, NetSerializable]
public sealed partial class RMCC4MultitoolDisarmDoAfterEvent : SimpleDoAfterEvent;
