using Content.Shared._RMC14.Ladder;
using Content.Shared._RMC14.Vehicle;
using Robust.Server.GameObjects;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.Player;
using Robust.Shared.Utility;
using System.Numerics;

namespace Content.Server._RMC14.Vehicle;
public sealed partial class VehicleSystem : SharedVehicleSystem
{
    [Dependency] private readonly MapLoaderSystem _mapLoader = default!;
    [Dependency] private readonly MapSystem _mapSystem = default!;
    [Dependency] private readonly SharedEyeSystem _eye = default!;
    [Dependency] private readonly ViewSubscriberSystem _viewSubscriber = default!;

    private EntityUid? _vehicleInteriorsMap = null;
    private int _interiorIndex = 0;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VehicleComponent, ComponentInit>(OnSpawnVehicleInterior);
        InitializeVehicleMovement();
    }

    private void OnSpawnVehicleInterior(Entity<VehicleComponent> ent, ref ComponentInit args)
    {
        if (!HasComp<VehicleEnterComponent>(ent))
            return; // No recursive map spawning please.

        if (_vehicleInteriorsMap is null)
            _vehicleInteriorsMap = _mapSystem.CreateMap();

        var vehicleInteriorsMapTransform = Transform((EntityUid)_vehicleInteriorsMap);
        var interior = new ResPath("/Maps/_RMC14/Vehicles/vehicle_testing_interior_driving.yml"); // TODO: Change this to use the VehicleComponent to make it possible to change the vehicle interrior.

        if (!_mapLoader.TryLoadGrid(vehicleInteriorsMapTransform.MapID, interior, out var interiorGrid, offset: new Vector2(_interiorIndex * 100, _interiorIndex * 100)))
            return;

        _interiorIndex++;

        var exitCompQuery = EntityQueryEnumerator<VehicleExitComponent>();

        while (exitCompQuery.MoveNext(out var uid, out var comp))
        {
            if (!TryComp<VehicleComponent>(uid, out var vehicleComp))
                continue;

            if (vehicleComp.Other is { } other)
                continue;

            //if (vehicleComp.Id == ent.Comp.Id)
            //    continue;

            ent.Comp.Other = uid;
            vehicleComp.Other = ent.Owner;
        }

        // TODO: Find the vehicle seat component and add the outside Vehicle Component to the VehicleDriverSeatComponent
        var driverSeatCompQuery = EntityQueryEnumerator<VehicleDriverSeatComponent>();

        while (driverSeatCompQuery.MoveNext(out var uid, out var comp))
        {
            if (!TryComp<VehicleDriverSeatComponent>(uid, out var driverSeatComp))
                continue;

            if (driverSeatComp.Vehicle is not null)
                continue;

            ent.Comp.DriverSeat = uid;
            driverSeatComp.Vehicle = ent.Owner;
        }

        Dirty(ent);
    }

    protected override void Watch(Entity<ActorComponent?, EyeComponent?> watcher, Entity<VehicleComponent?> toWatch)
    {
        base.Watch(watcher, toWatch);

        if (!Resolve(toWatch, ref toWatch.Comp, false))
            return;

        if (watcher.Owner == toWatch.Owner)
            return;

        if (!Resolve(watcher, ref watcher.Comp1, ref watcher.Comp2) ||
            !Resolve(toWatch, ref toWatch.Comp))
            return;

        _eye.SetTarget(watcher, toWatch, watcher);
        _viewSubscriber.AddViewSubscriber(toWatch, watcher.Comp1.PlayerSession);

        RemoveWatcher(watcher);
        EnsureComp<VehicleWatchingComponent>(watcher).Watching = toWatch;
        toWatch.Comp.Watching.Add(watcher);
    }

    protected override void Unwatch(Entity<EyeComponent?> watcher, ICommonSession player)
    {
        if (!Resolve(watcher, ref watcher.Comp))
            return;

        var oldTarget = watcher.Comp.Target;

        base.Unwatch(watcher, player);

        if (oldTarget != null && oldTarget != watcher.Owner)
            _viewSubscriber.RemoveViewSubscriber(oldTarget.Value, player);

        RemoveWatcher(watcher);
    }

    private void RemoveWatcher(EntityUid toRemove)
    {
        if (!TryComp(toRemove, out VehicleWatchingComponent? watching))
            return;

        if (TryComp(watching.Watching, out VehicleComponent? watched))
            watched.Watching.Remove(toRemove);

        watching.Watching = null;
        RemCompDeferred<VehicleWatchingComponent>(toRemove);
    }
}
