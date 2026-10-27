using Content.Server.Power.Components;
using Content.Shared._RMC14.Power;
using Content.Shared.Examine;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Power;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Server._RMC14.Power;

public sealed class RMCFusionFuelRecyclerSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly RMCPowerSystem _power = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private readonly HashSet<EntityUid> _tracked = new();
    private EntityQuery<ApcPowerReceiverComponent> _apcReceiverQuery;
    private EntityQuery<RMCFusionCellComponent> _cellQuery;
    private EntityQuery<RMCPowerReceiverComponent> _powerReceiverQuery;
    private EntityQuery<RMCFusionFuelRecyclerComponent> _recyclerQuery;

    public override void Initialize()
    {
        _apcReceiverQuery = GetEntityQuery<ApcPowerReceiverComponent>();
        _cellQuery = GetEntityQuery<RMCFusionCellComponent>();
        _powerReceiverQuery = GetEntityQuery<RMCPowerReceiverComponent>();
        _recyclerQuery = GetEntityQuery<RMCFusionFuelRecyclerComponent>();

        SubscribeLocalEvent<RMCFusionFuelRecyclerComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<RMCFusionFuelRecyclerComponent, ComponentRemove>(OnRemove);
        SubscribeLocalEvent<RMCFusionFuelRecyclerComponent, EntityTerminatingEvent>(OnRemove);
        SubscribeLocalEvent<RMCFusionFuelRecyclerComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<RMCFusionFuelRecyclerComponent, InteractHandEvent>(OnInteractHand);
        SubscribeLocalEvent<RMCFusionFuelRecyclerComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<RMCFusionCellComponent, MapInitEvent>(OnCellMapInit);
        SubscribeLocalEvent<RMCFusionCellComponent, ExaminedEvent>(OnCellExamined);
    }

    private void OnCellMapInit(Entity<RMCFusionCellComponent> ent, ref MapInitEvent args)
    {
        UpdateCellAppearance(ent);
    }

    private void OnCellExamined(Entity<RMCFusionCellComponent> ent, ref ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("rmc-fusion-cell-examine",
            ("percent", MathF.Floor(ent.Comp.FuelPercentage * 100))));
    }

    private void OnMapInit(Entity<RMCFusionFuelRecyclerComponent> ent, ref MapInitEvent args)
    {
        _tracked.Add(ent);
        _container.EnsureContainer<ContainerSlot>(ent, ent.Comp.LeftSlot);
        _container.EnsureContainer<ContainerSlot>(ent, ent.Comp.RightSlot);
        var left = GetCell(_container.EnsureContainer<ContainerSlot>(ent, ent.Comp.LeftSlot));
        var right = GetCell(_container.EnsureContainer<ContainerSlot>(ent, ent.Comp.RightSlot));
        if (left is { } leftCell)
            UpdateCellAppearance(leftCell);
        if (right is { } rightCell)
            UpdateCellAppearance(rightCell);
        UpdateState(ent);
    }

    private void OnRemove<T>(Entity<RMCFusionFuelRecyclerComponent> ent, ref T args)
    {
        _tracked.Remove(ent);
    }

    private void OnInteractUsing(Entity<RMCFusionFuelRecyclerComponent> ent, ref InteractUsingEvent args)
    {
        if (!_cellQuery.TryComp(args.Used, out var cell))
            return;

        args.Handled = true;
        if (cell.Fuel >= cell.MaxFuel)
        {
            _popup.PopupClient(Loc.GetString("rmc-fusion-recycler-cell-full"), ent, args.User, PopupType.SmallCaution);
            return;
        }

        var left = _container.EnsureContainer<ContainerSlot>(ent, ent.Comp.LeftSlot);
        var right = _container.EnsureContainer<ContainerSlot>(ent, ent.Comp.RightSlot);
        var target = left.ContainedEntity == null
            ? left
            : right.ContainedEntity == null
                ? right
                : null;
        if (target == null)
        {
            _popup.PopupClient(Loc.GetString("rmc-fusion-recycler-slots-full"), ent, args.User, PopupType.SmallCaution);
            return;
        }

        if (!_container.Insert(args.Used, target))
            return;

        _popup.PopupClient(Loc.GetString("rmc-fusion-recycler-insert", ("cell", args.Used)), ent, args.User);
        UpdateState(ent);
    }

    private void OnInteractHand(Entity<RMCFusionFuelRecyclerComponent> ent, ref InteractHandEvent args)
    {
        var left = _container.EnsureContainer<ContainerSlot>(ent, ent.Comp.LeftSlot);
        var right = _container.EnsureContainer<ContainerSlot>(ent, ent.Comp.RightSlot);
        var leftCell = GetCell(left);
        var rightCell = GetCell(right);
        Entity<RMCFusionCellComponent>? selected = (leftCell, rightCell) switch
        {
            (null, null) => null,
            ({ } cell, null) => cell,
            (null, { } cell) => cell,
            ({ } leftValue, { } rightValue) => leftValue.Comp.Fuel >= rightValue.Comp.Fuel
                ? leftValue
                : rightValue,
        };
        if (selected == null)
            return;

        args.Handled = true;
        if (_container.TryGetContainingContainer(selected.Value.Owner, out var containing))
            _container.Remove(selected.Value.Owner, containing);
        _hands.TryPickupAnyHand(args.User, selected.Value.Owner);
        _popup.PopupClient(Loc.GetString("rmc-fusion-recycler-remove", ("cell", selected.Value.Owner)),
            ent,
            args.User);
        UpdateState(ent);
    }

    private void OnExamined(Entity<RMCFusionFuelRecyclerComponent> ent, ref ExaminedEvent args)
    {
        var left = GetCell(_container.EnsureContainer<ContainerSlot>(ent, ent.Comp.LeftSlot));
        var right = GetCell(_container.EnsureContainer<ContainerSlot>(ent, ent.Comp.RightSlot));
        args.PushMarkup(Loc.GetString("rmc-fusion-recycler-examine",
            ("status", Loc.GetString(ent.Comp.Working
                ? "rmc-fusion-recycler-online"
                : "rmc-fusion-recycler-offline")),
            ("left", FormatFuel(left)),
            ("right", FormatFuel(right))));
    }

    public override void Update(float frameTime)
    {
        var now = _timing.CurTime;
        _tracked.RemoveWhere(uid => TerminatingOrDeleted(uid) || !_recyclerQuery.HasComp(uid));
        foreach (var uid in _tracked)
        {
            if (!_recyclerQuery.TryComp(uid, out var recycler))
                continue;

            var ent = new Entity<RMCFusionFuelRecyclerComponent>(uid, recycler);
            var left = GetCell(_container.EnsureContainer<ContainerSlot>(ent, recycler.LeftSlot));
            var right = GetCell(_container.EnsureContainer<ContainerSlot>(ent, recycler.RightSlot));
            var hasWork = CanRefuel(left) || CanRefuel(right);
            if (_powerReceiverQuery.TryComp(uid, out var receiver))
                _power.SetPowerMode((uid, receiver), hasWork ? RMCPowerMode.Active : RMCPowerMode.Idle);

            var powered = _apcReceiverQuery.TryComp(uid, out var apcReceiver) && apcReceiver.Powered;
            var working = hasWork && powered;
            if (recycler.Working != working)
            {
                recycler.Working = working;
                Dirty(uid, recycler);
            }

            if (!working)
            {
                recycler.NextProcessAt = TimeSpan.Zero;
                UpdateState(ent);
                continue;
            }

            if (recycler.NextProcessAt == TimeSpan.Zero)
                recycler.NextProcessAt = now + recycler.ProcessInterval;

            if (now < recycler.NextProcessAt)
            {
                UpdateState(ent);
                continue;
            }

            recycler.NextProcessAt = now + recycler.ProcessInterval;
            RefuelCell(left, recycler);
            RefuelCell(right, recycler);
            UpdateState(ent);
        }
    }

    private Entity<RMCFusionCellComponent>? GetCell(ContainerSlot slot)
    {
        return slot.ContainedEntity is { } uid && _cellQuery.TryComp(uid, out var cell)
            ? (uid, cell)
            : null;
    }

    private static bool CanRefuel(Entity<RMCFusionCellComponent>? cell)
    {
        return cell is { } value && value.Comp.Fuel < value.Comp.MaxFuel;
    }

    private void RefuelCell(Entity<RMCFusionCellComponent>? cell, RMCFusionFuelRecyclerComponent recycler)
    {
        if (!CanRefuel(cell))
            return;

        var value = cell!.Value;
        var wasFull = value.Comp.Fuel >= value.Comp.MaxFuel;
        _power.AddFusionCellFuel(value, recycler.FuelPerCycle);
        UpdateCellAppearance(value);
        if (!wasFull && value.Comp.Fuel >= value.Comp.MaxFuel)
            _audio.PlayPvs(recycler.FinishSound, value);
    }

    private void UpdateState(Entity<RMCFusionFuelRecyclerComponent> ent)
    {
        var left = GetCell(_container.EnsureContainer<ContainerSlot>(ent, ent.Comp.LeftSlot));
        var right = GetCell(_container.EnsureContainer<ContainerSlot>(ent, ent.Comp.RightSlot));
        _appearance.SetData(ent, RMCFusionFuelRecyclerVisuals.Working, ent.Comp.Working);
        _appearance.SetData(ent, RMCFusionFuelRecyclerVisuals.LeftCell, left != null);
        _appearance.SetData(ent, RMCFusionFuelRecyclerVisuals.RightCell, right != null);
        _appearance.SetData(ent, RMCFusionFuelRecyclerVisuals.LeftCharging, ent.Comp.Working && CanRefuel(left));
        _appearance.SetData(ent, RMCFusionFuelRecyclerVisuals.RightCharging, ent.Comp.Working && CanRefuel(right));
        _appearance.SetData(ent, RMCFusionFuelRecyclerVisuals.LeftCharged,
            left is { } leftCell && leftCell.Comp.Fuel >= leftCell.Comp.MaxFuel);
        _appearance.SetData(ent, RMCFusionFuelRecyclerVisuals.RightCharged,
            right is { } rightCell && rightCell.Comp.Fuel >= rightCell.Comp.MaxFuel);
    }

    private void UpdateCellAppearance(Entity<RMCFusionCellComponent> cell)
    {
        var level = cell.Comp.FuelPercentage switch
        {
            >= 1 => RMCFusionCellFuelLevel.Full,
            >= 0.75f => RMCFusionCellFuelLevel.High,
            >= 0.25f => RMCFusionCellFuelLevel.Medium,
            > 0 => RMCFusionCellFuelLevel.Low,
            _ => RMCFusionCellFuelLevel.Empty,
        };
        _appearance.SetData(cell, RMCFusionCellVisuals.Fuel, level);
    }

    private static string FormatFuel(Entity<RMCFusionCellComponent>? cell)
    {
        return cell is { } value
            ? $"{MathF.Round(value.Comp.Fuel)}/{MathF.Round(value.Comp.MaxFuel)}"
            : "—";
    }
}
