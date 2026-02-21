using Content.Shared._RMC14.Vehicle.Viewport;
using Content.Shared.Movement.Events;
using Robust.Shared.GameObjects;

namespace Content.Client._RMC14.Vehicle.Viewport;

public sealed class RMCVehicleViewportSystem : EntitySystem
{
    [Dependency] private readonly SharedEyeSystem _eye = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RMCVehicleViewportUserComponent, MoveInputEvent>(OnUserMove);
    }

    private void OnUserMove(Entity<RMCVehicleViewportUserComponent> ent, ref MoveInputEvent args)
    {
        if (!args.HasDirectionalMovement)
            return;

        if (TryComp(ent, out EyeComponent? eye))
            _eye.SetTarget(ent.Owner, ent.Comp.PreviousTarget, eye);
    }
}
