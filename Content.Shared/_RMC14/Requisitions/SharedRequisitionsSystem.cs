using Content.Shared._RMC14.Requisitions.Components;
using Content.Shared.Climbing.Components;
using Content.Shared.StepTrigger.Systems;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;
using static Content.Shared._RMC14.Requisitions.Components.RequisitionsRailingMode;

namespace Content.Shared._RMC14.Requisitions;

public abstract class SharedRequisitionsSystem : EntitySystem
{
    [Dependency] private readonly FixtureSystem _fixtures = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RequisitionsElevatorComponent, StepTriggerAttemptEvent>(OnElevatorStepTriggerAttempt);

        SubscribeLocalEvent<RequisitionsRailingComponent, MapInitEvent>(OnRailingMapInit);
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
}
