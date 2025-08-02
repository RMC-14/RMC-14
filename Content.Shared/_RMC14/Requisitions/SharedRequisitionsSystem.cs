using Content.Shared._RMC14.CCVar;
using Content.Shared._RMC14.Requisitions.Components;
using Content.Shared._RMC14.Scaling;
using Content.Shared.Climbing.Components;
using Content.Shared.GameTicking;
using Content.Shared.StepTrigger.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Prototypes;
using System.Numerics;
using static Content.Shared._RMC14.Requisitions.Components.RequisitionsRailingMode;

namespace Content.Shared._RMC14.Requisitions;

public abstract class SharedRequisitionsSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly FixtureSystem _fixtures = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;

    public int Starting { get; private set; }
    public int StartingDollarsPerMarine { get; private set; }
    public int PointsScale { get; private set; }


    private MapId? _purchasesMap;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestartCleanup);

        SubscribeLocalEvent<MarineScaleChangedEvent>(OnMarineScaleChanged);

        SubscribeLocalEvent<RequisitionsElevatorComponent, StepTriggerAttemptEvent>(OnElevatorStepTriggerAttempt);

        SubscribeLocalEvent<RequisitionsRailingComponent, MapInitEvent>(OnRailingMapInit);

        Subs.CVar(_config, RMCCVars.RMCRequisitionsStartingBalance, v => Starting = v, true);
        Subs.CVar(_config, RMCCVars.RMCRequisitionsStartingDollarsPerMarine, v => StartingDollarsPerMarine = v, true);
        Subs.CVar(_config, RMCCVars.RMCRequisitionsPointsScale, v => PointsScale = v, true);
    }

    private void OnMarineScaleChanged(ref MarineScaleChangedEvent ev)
    {
        if (ev.Delta <= 0)
            return;

        var accounts = EntityQueryEnumerator<RequisitionsAccountComponent>();
        while (accounts.MoveNext(out var uid, out var account))
        {
            // TODO RMC14 initial money should depend on the scale too
            account.Balance += (int) ev.Delta * PointsScale;
            Dirty(uid, account);
        }
    }

    private void OnElevatorStepTriggerAttempt(Entity<RequisitionsElevatorComponent> elevator, ref StepTriggerAttemptEvent args)
    {
        if (elevator.Comp.Mode == RequisitionsElevatorMode.Raised)
            args.Cancelled = true;
    }

    private void OnRailingMapInit(Entity<RequisitionsRailingComponent> railing, ref MapInitEvent args)
    {
        UpdateRailing(railing);
    }

    private void UpdateRailing(Entity<RequisitionsRailingComponent> railing)
    {
        if (!TryComp(railing, out FixturesComponent? fixtures) ||
            _fixtures.GetFixtureOrNull(railing, railing.Comp.Fixture, fixtures) is not { } fixture)
        {
            return;
        }

        var hard = railing.Comp.Mode is Raising or Raised;
        _physics.SetHard(railing, fixture, hard);

        if (hard)
            EnsureComp<ClimbableComponent>(railing);
        else
            RemCompDeferred<ClimbableComponent>(railing);
    }

    protected void SetRailingMode(Entity<RequisitionsRailingComponent> railing, RequisitionsRailingMode mode)
    {
        if (railing.Comp.Mode == mode)
            return;

        railing.Comp.Mode = mode;
        Dirty(railing);

        UpdateRailing(railing);
    }

    public void ChangeBudget(int amount)
    {
        var accountQuery = EntityQueryEnumerator<RequisitionsAccountComponent>();
        while (accountQuery.MoveNext(out var uid, out var comp))
        {
            comp.Balance += amount;
            Dirty(uid, comp);
        }

        SendUIStateAll();
    }

    protected void SendUIStateAll()
    {
        var query = EntityQueryEnumerator<RequisitionsComputerComponent>();
        while (query.MoveNext(out var uid, out var computer))
        {
            SendUIState((uid, computer));
        }
    }

    protected void SendUIState(Entity<RequisitionsComputerComponent> computer)
    {
        var elevator = GetElevator(computer);
        var mode = elevator?.Comp.NextMode ?? elevator?.Comp.Mode;
        var busy = elevator?.Comp.Busy ?? false;
        var balance = CompOrNull<RequisitionsAccountComponent>(computer.Comp.Account)?.Balance ?? 0;
        var full = elevator != null && IsFull(elevator.Value);

        var state = new RequisitionsBuiState(mode, busy, balance, full);
        _ui.SetUiState(computer.Owner, RequisitionsUIKey.Key, state);
    }

    protected bool IsFull(Entity<RequisitionsElevatorComponent> elevator)
    {
        return elevator.Comp.Orders.Count >= GetElevatorCapacity(elevator);
    }

    protected int GetElevatorCapacity(Entity<RequisitionsElevatorComponent> elevator)
    {
        var side = (int) MathF.Floor(elevator.Comp.Radius * 2 + 1);
        return side * side;
    }

    protected Entity<RequisitionsElevatorComponent>? GetElevator(Entity<RequisitionsComputerComponent> computer)
    {
        var elevators = new List<Entity<RequisitionsElevatorComponent, TransformComponent>>();
        var query = EntityQueryEnumerator<RequisitionsElevatorComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var elevator, out var xform))
        {
            elevators.Add((uid, elevator, xform));
        }

        if (elevators.Count == 0)
            return null;

        if (elevators.Count == 1)
            return elevators[0];

        var computerCoords = _transform.GetMapCoordinates(computer);
        Entity<RequisitionsElevatorComponent>? closest = null;
        var closestDistance = float.MaxValue;
        foreach (var (uid, elevator, xform) in elevators)
        {
            var elevatorCoords = _transform.GetMapCoordinates(uid, xform);
            if (computerCoords.MapId != elevatorCoords.MapId)
                continue;

            var distance = (elevatorCoords.Position - computerCoords.Position).LengthSquared();
            if (closestDistance > distance)
            {
                closestDistance = distance;
                closest = (uid, elevator);
            }
        }

        if (closest == null)
            return elevators[0];

        return closest;
    }

    public void StartAccount(Entity<RequisitionsAccountComponent> account, double scale, float marines)
    {
        if (account.Comp.Started)
            return;

        account.Comp.Started = true;

        var startingPoints = Starting;
        var scalePoints = (int) (PointsScale * scale);
        var perMarinePoints = (int) (StartingDollarsPerMarine * marines);
        account.Comp.Balance = startingPoints + scalePoints + perMarinePoints;

        Dirty(account);
    }

    private void OnRoundRestartCleanup(RoundRestartCleanupEvent ev)
    {
        _purchasesMap = null;
    }

    public void CreateSpecialDelivery(EntProtoId proto)
    {
        var map = EnsurePurchasesMap();
        var delivery = Spawn(proto, new MapCoordinates(Vector2.Zero, map));
        EnsureComp<RequisitionsCustomDeliveryComponent>(delivery);
    }

    private MapId EnsurePurchasesMap()
    {
        if (_purchasesMap != null)
            return _purchasesMap.Value;

        _map.CreateMap(out var map);
        _purchasesMap = map;
        return map;
    }
}
