using Content.Server.Power.EntitySystems;
using Content.Shared._RMC14.Synth;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Climbing.Systems;
using Content.Shared.Damage;
using Content.Shared.Destructible;
using Content.Shared.DoAfter;
using Content.Shared.DragDrop;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Movement.Events;
using Content.Shared.Popups;
using Content.Shared.Stunnable;
using Content.Shared.Verbs;
using Robust.Server.Containers;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Server._RMC14.Synth;

/// <summary>
/// Server-side behavior for synthetic maintenance station insertion, repair, charge, and ejection.
/// </summary>
public sealed class RMCSyntheticMaintenanceStationSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedBloodstreamSystem _bloodstream = default!;
    [Dependency] private readonly ClimbSystem _climb = default!;
    [Dependency] private readonly ContainerSystem _container = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCSyntheticMaintenanceStationComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<RMCSyntheticMaintenanceStationComponent, ContainerRelayMovementEntityEvent>(OnRelayMovement);
        SubscribeLocalEvent<RMCSyntheticMaintenanceStationComponent, GetVerbsEvent<InteractionVerb>>(OnGetInteractionVerbs);
        SubscribeLocalEvent<RMCSyntheticMaintenanceStationComponent, GetVerbsEvent<AlternativeVerb>>(OnGetAlternativeVerbs);
        SubscribeLocalEvent<RMCSyntheticMaintenanceStationComponent, DragDropTargetEvent>(OnDragDrop);
        SubscribeLocalEvent<RMCSyntheticMaintenanceStationComponent, DestructionEventArgs>(OnDestroyed);
        SubscribeLocalEvent<RMCSyntheticMaintenanceStationComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<RMCSyntheticMaintenanceStationComponent, RMCSyntheticMaintenanceStationInsertDoAfterEvent>(OnInsertDoAfter);
    }

    private void OnComponentInit(Entity<RMCSyntheticMaintenanceStationComponent> ent, ref ComponentInit args)
    {
        ent.Comp.BodyContainer = _container.EnsureContainer<ContainerSlot>(ent, RMCSyntheticMaintenanceStationComponent.BodyContainerId);
        ent.Comp.CurrentInternalCharge = Math.Min(ent.Comp.CurrentInternalCharge, ent.Comp.MaxInternalCharge);
        ent.Comp.NextUpdate = _timing.CurTime + ent.Comp.UpdateInterval;
        UpdateAppearance(ent);
    }

    private void OnRelayMovement(Entity<RMCSyntheticMaintenanceStationComponent> ent, ref ContainerRelayMovementEntityEvent args)
    {
        // Moving while contained is treated as leaving the station.
        Eject(ent);
    }

    private void OnGetInteractionVerbs(Entity<RMCSyntheticMaintenanceStationComponent> ent, ref GetVerbsEvent<InteractionVerb> args)
    {
        if (args.Using == null ||
            !args.CanAccess ||
            !args.CanInteract ||
            !CanInsert(ent, args.Using.Value))
        {
            return;
        }

        var target = args.Using.Value;
        var user = args.User;
        args.Verbs.Add(new InteractionVerb
        {
            Act = () => StartInsertDoAfter(ent, user, target),
            Category = VerbCategory.Insert,
            Text = Name(target),
        });
    }

    private void OnGetAlternativeVerbs(Entity<RMCSyntheticMaintenanceStationComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        if (IsOccupied(ent.Comp))
        {
            args.Verbs.Add(new AlternativeVerb
            {
                Act = () => Eject(ent),
                Category = VerbCategory.Eject,
                Text = Loc.GetString("rmc-synthetic-maintenance-station-eject-verb"),
                Priority = 1,
            });
            return;
        }

        if (!CanInsert(ent, args.User))
            return;

        var user = args.User;
        args.Verbs.Add(new AlternativeVerb
        {
            Act = () => Insert(ent, user),
            Category = VerbCategory.Insert,
            Text = Loc.GetString("rmc-synthetic-maintenance-station-enter-verb"),
        });
    }

    private void OnDragDrop(Entity<RMCSyntheticMaintenanceStationComponent> ent, ref DragDropTargetEvent args)
    {
        if (args.Handled || !CanInsert(ent, args.Dragged))
            return;

        // Self-insertion is instant; putting another synth into the station uses the CM-style do-after.
        if (args.User == args.Dragged)
            args.Handled = Insert(ent, args.Dragged);
        else
            args.Handled = StartInsertDoAfter(ent, args.User, args.Dragged);
    }

    private void OnDestroyed(Entity<RMCSyntheticMaintenanceStationComponent> ent, ref DestructionEventArgs args)
    {
        Eject(ent);
    }

    private void OnExamined(Entity<RMCSyntheticMaintenanceStationComponent> ent, ref ExaminedEvent args)
    {
        var percent = ent.Comp.MaxInternalCharge <= 0
            ? 0
            : Math.Round(ent.Comp.CurrentInternalCharge / ent.Comp.MaxInternalCharge * 100);

        using (args.PushGroup(nameof(RMCSyntheticMaintenanceStationComponent)))
        {
            args.PushMarkup(Loc.GetString("rmc-synthetic-maintenance-station-charge-examine", ("charge", percent)));
        }
    }

    private void OnInsertDoAfter(Entity<RMCSyntheticMaintenanceStationComponent> ent, ref RMCSyntheticMaintenanceStationInsertDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        args.Handled = true;
        var inserted = GetEntity(args.Inserted);
        Insert(ent, inserted);
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<RMCSyntheticMaintenanceStationComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (_timing.CurTime < comp.NextUpdate)
                continue;

            comp.NextUpdate += comp.UpdateInterval;
            Process((uid, comp));
        }
    }

    private void Process(Entity<RMCSyntheticMaintenanceStationComponent> ent)
    {
        var powered = this.IsPowered(ent.Owner, EntityManager);
        var oldCharge = ent.Comp.CurrentInternalCharge;

        // The station stores its own charge: power only changes recharge/drain, while repairs spend stored charge.
        var occupied = ent.Comp.BodyContainer.ContainedEntity != null;
        var rechargeRate = occupied ? ent.Comp.ActiveRechargeRate : ent.Comp.PassiveRechargeRate;
        if (powered)
            ent.Comp.CurrentInternalCharge = Math.Min(ent.Comp.MaxInternalCharge, ent.Comp.CurrentInternalCharge + rechargeRate);
        else
            ent.Comp.CurrentInternalCharge = Math.Max(0, ent.Comp.CurrentInternalCharge - ent.Comp.UnpoweredDrainRate);

        if (ent.Comp.BodyContainer.ContainedEntity is not { } contained)
        {
            if (Math.Abs(oldCharge - ent.Comp.CurrentInternalCharge) > 0.01f)
                UpdateAppearance(ent);

            return;
        }

        if (!powered || ent.Comp.CurrentInternalCharge <= 0)
        {
            // CM ejects occupants when the station cannot keep running.
            _popup.PopupEntity(Loc.GetString("rmc-synthetic-maintenance-station-power-fail"), ent, contained);
            Eject(ent);
            return;
        }

        // Repair damage first, then restore synthetic blood/fluid, then eject once maintenance is complete.
        if (TryRepair(contained, ent))
            ent.Comp.CurrentInternalCharge = Math.Max(0, ent.Comp.CurrentInternalCharge - ent.Comp.RepairChargeCost);
        else if (TryRestoreBlood(contained, ent))
            ent.Comp.CurrentInternalCharge = Math.Max(0, ent.Comp.CurrentInternalCharge - ent.Comp.RepairChargeCost);
        else
        {
            _popup.PopupEntity(Loc.GetString("rmc-synthetic-maintenance-station-complete"), ent, contained);
            Eject(ent);
        }

        UpdateAppearance(ent);
    }

    private bool TryRepair(EntityUid contained, Entity<RMCSyntheticMaintenanceStationComponent> station)
    {
        if (!TryComp(contained, out DamageableComponent? damageable) ||
            damageable.TotalDamage <= FixedPoint2.Zero)
        {
            return false;
        }

        // The component stores negative damage so the normal damage system handles all repair math.
        return _damageable.TryChangeDamage(contained,
            station.Comp.RepairDamage,
            ignoreResistances: true,
            interruptsDoAfters: false,
            damageable: damageable,
            origin: station.Owner) != null;
    }

    private bool TryRestoreBlood(EntityUid contained, Entity<RMCSyntheticMaintenanceStationComponent> station)
    {
        if (!TryComp(contained, out BloodstreamComponent? bloodstream) ||
            _bloodstream.GetBloodLevelPercentage((contained, bloodstream)) >= 0.999f)
        {
            return false;
        }

        return _bloodstream.TryModifyBloodLevel((contained, bloodstream), station.Comp.BloodRestoreAmount);
    }

    private bool StartInsertDoAfter(Entity<RMCSyntheticMaintenanceStationComponent> station, EntityUid user, EntityUid target)
    {
        if (!CanInsert(station, target))
            return false;

        var ev = new RMCSyntheticMaintenanceStationInsertDoAfterEvent(GetNetEntity(target));
        var doAfter = new DoAfterArgs(EntityManager, user, station.Comp.InsertDelay, ev, station, target)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true,
            DuplicateCondition = DuplicateConditions.SameEvent,
        };

        return _doAfter.TryStartDoAfter(doAfter);
    }

    private bool Insert(Entity<RMCSyntheticMaintenanceStationComponent> station, EntityUid target)
    {
        if (!CanInsert(station, target))
            return false;

        if (!_container.Insert(target, station.Comp.BodyContainer))
            return false;

        _popup.PopupEntity(Loc.GetString("rmc-synthetic-maintenance-station-enter", ("target", target)), station.Owner);
        UpdateAppearance(station);
        return true;
    }

    private void Eject(Entity<RMCSyntheticMaintenanceStationComponent> station)
    {
        if (station.Comp.BodyContainer.ContainedEntity is not { } contained)
            return;

        // Climb positioning gives the occupant a normal adjacent ejection path instead of raw coordinate teleporting.
        _container.Remove(contained, station.Comp.BodyContainer);
        _climb.ForciblySetClimbing(contained, station);
        if (station.Comp.ExitStun > TimeSpan.Zero)
            _stun.TryStun(contained, station.Comp.ExitStun, true);

        UpdateAppearance(station);
    }

    private bool CanInsert(Entity<RMCSyntheticMaintenanceStationComponent> station, EntityUid target)
    {
        if (IsOccupied(station.Comp) ||
            !HasComp<SynthComponent>(target) ||
            TerminatingOrDeleted(target))
        {
            return false;
        }

        return _container.CanInsert(target, station.Comp.BodyContainer);
    }

    private bool IsOccupied(RMCSyntheticMaintenanceStationComponent component)
    {
        return component.BodyContainer.ContainedEntity != null;
    }

    private void UpdateAppearance(Entity<RMCSyntheticMaintenanceStationComponent> ent)
    {
        if (!TryComp(ent, out AppearanceComponent? appearance))
            return;

        var occupied = IsOccupied(ent.Comp);
        if (ent.Comp.Occupied != occupied)
        {
            // Keep the shared drop-target state in sync with the server-only container.
            ent.Comp.Occupied = occupied;
            Dirty(ent);
        }

        var status = !this.IsPowered(ent.Owner, EntityManager) || ent.Comp.CurrentInternalCharge <= 0
            ? RMCSyntheticMaintenanceStationStatus.Off
            : occupied
                ? RMCSyntheticMaintenanceStationStatus.Occupied
                : RMCSyntheticMaintenanceStationStatus.Empty;

        _appearance.SetData(ent, RMCSyntheticMaintenanceStationVisuals.Status, status, appearance);
        _appearance.SetData(ent, RMCSyntheticMaintenanceStationVisuals.Charge, GetCharge(ent), appearance);
    }

    private RMCSyntheticMaintenanceStationCharge GetCharge(Entity<RMCSyntheticMaintenanceStationComponent> ent)
    {
        if (ent.Comp.MaxInternalCharge <= 0)
            return RMCSyntheticMaintenanceStationCharge.Empty;

        var fraction = ent.Comp.CurrentInternalCharge / ent.Comp.MaxInternalCharge;
        return fraction switch
        {
            <= 0.05f => RMCSyntheticMaintenanceStationCharge.Empty,
            <= 0.35f => RMCSyntheticMaintenanceStationCharge.Low,
            <= 0.65f => RMCSyntheticMaintenanceStationCharge.Medium,
            <= 0.85f => RMCSyntheticMaintenanceStationCharge.High,
            _ => RMCSyntheticMaintenanceStationCharge.Full,
        };
    }
}
