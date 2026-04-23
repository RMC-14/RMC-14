using Content.Client._RMC14.Buckle;
using Content.Client._RMC14.Sprite;
using Content.Client._RMC14.Xenonids;
using Content.Client._RMC14.Xenonids.Hide;
using Content.Shared._RMC14.Sprite;
using Content.Shared.Vehicle;
using Content.Shared.Vehicle.Components;
using RmcDrawDepth = Content.Shared.DrawDepth.DrawDepth;
using Robust.Client.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Player;

namespace Content.Client._RMC14.Vehicle;

public sealed class VehicleRideSurfaceVisualSystem : EntitySystem
{
    private const float RiderPositionEpsilon = 0.000001f;

    [Dependency] private readonly ISharedPlayerManager _player = default!;
    [Dependency] private readonly RMCSpriteSystem _rmcSprite = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        UpdatesAfter.Add(typeof(RMCSpriteSystem));
        UpdatesAfter.Add(typeof(VehicleRideSurfaceSystem));

        SubscribeLocalEvent<VehicleRideSurfaceRiderComponent, AfterAutoHandleStateEvent>(OnRiderState);
        SubscribeLocalEvent<VehicleRideSurfaceRiderComponent, GetDrawDepthEvent>(
            OnGetDrawDepth,
            after: [typeof(XenoHideVisualizerSystem), typeof(XenoVisualizerSystem), typeof(RMCBuckleVisualsSystem)]);

        EntityManager.ComponentRemoved += OnComponentRemoved;
    }

    public override void Shutdown()
    {
        base.Shutdown();
        EntityManager.ComponentRemoved -= OnComponentRemoved;
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<VehicleRideSurfaceRiderComponent, SpriteComponent>();
        while (query.MoveNext(out var uid, out var rider, out var sprite))
        {
            UpdateDrawDepth((uid, rider, sprite));
        }
    }

    public override void FrameUpdate(float frameTime)
    {
        var query = EntityQueryEnumerator<VehicleRideSurfaceRiderComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var rider, out var xform))
        {
            PredictRemoteRiderPosition((uid, rider, xform));
        }
    }

    private void PredictRemoteRiderPosition(Entity<VehicleRideSurfaceRiderComponent, TransformComponent> ent)
    {
        if (ent.Owner == _player.LocalEntity)
            return;

        if (!TryComp(ent.Comp1.Vehicle, out TransformComponent? vehicleXform))
            return;

        var (vehiclePosition, vehicleRotation) = _transform.GetWorldPositionRotation(vehicleXform);
        var targetPosition = vehiclePosition + vehicleRotation.RotateVec(ent.Comp1.LocalPosition);
        var riderMap = _transform.GetMapCoordinates((ent.Owner, ent.Comp2));
        if (riderMap.MapId != vehicleXform.MapID)
            return;

        if ((targetPosition - riderMap.Position).LengthSquared() <= RiderPositionEpsilon)
            return;

        ent.Comp2.ActivelyLerping = false;
        _transform.SetMapCoordinates((ent.Owner, ent.Comp2), new MapCoordinates(targetPosition, riderMap.MapId));
    }

    private void OnRiderState(Entity<VehicleRideSurfaceRiderComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        _rmcSprite.UpdateDrawDepth(ent.Owner);
    }

    private void OnComponentRemoved(RemovedComponentEventArgs args)
    {
        if (args.Terminating || args.BaseArgs.Component is not VehicleRideSurfaceRiderComponent)
            return;

        _rmcSprite.UpdateDrawDepth(args.BaseArgs.Owner);
    }

    private void OnGetDrawDepth(Entity<VehicleRideSurfaceRiderComponent> ent, ref GetDrawDepthEvent args)
    {
        args.DrawDepth = (RmcDrawDepth) GetRiderDrawDepth(ent.Comp);
    }

    private void UpdateDrawDepth(Entity<VehicleRideSurfaceRiderComponent, SpriteComponent> ent)
    {
        var drawDepth = GetRiderDrawDepth(ent.Comp1);

        if (ent.Comp2.DrawDepth != drawDepth)
            _sprite.SetDrawDepth((ent.Owner, ent.Comp2), drawDepth);
    }

    private int GetRiderDrawDepth(VehicleRideSurfaceRiderComponent rider)
    {
        if (TryComp(rider.Vehicle, out VehicleRideSurfaceComponent? surface))
            return surface.RiderDrawDepth;

        return (int) RmcDrawDepth.OverMobs + 2;
    }
}
