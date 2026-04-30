using Content.Shared.Explosion.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Tag;
using Robust.Shared.GameObjects;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Vehicle;

public sealed class VehicleEntranceItemSystem : EntitySystem
{
    private static readonly ProtoId<TagPrototype> HandGrenadeTag = "HandGrenade";
    private static readonly ProtoId<TagPrototype> GrenadeTag = "Grenade";

    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly VehicleSystem _vehicle = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<AfterInteractUsingEvent>(OnAfterInteractUsing);
    }

    private void OnInteractUsing(InteractUsingEvent args)
    {
        if (_net.IsClient || args.Handled)
            return;

        args.Handled = TryInsertPrimedGrenade(args.Target, args.User, args.Used);
    }

    private void OnAfterInteractUsing(AfterInteractUsingEvent args)
    {
        if (_net.IsClient || args.Handled)
            return;

        if (args.Target is not { } target)
            return;

        args.Handled = TryInsertPrimedGrenade(target, args.User, args.Used);
    }

    private bool TryInsertPrimedGrenade(EntityUid target, EntityUid user, EntityUid used)
    {
        var vehicle = ResolveVehicleTarget(target);
        if (vehicle == EntityUid.Invalid)
            return false;

        if (!TryComp<VehicleEnterComponent>(vehicle, out _))
            return false;

        if (!IsPrimedHandGrenade(used))
            return false;

        if (TryComp<VehicleLockComponent>(vehicle, out var vehicleLock) &&
            vehicleLock.Locked &&
            !vehicleLock.Broken)
        {
            return false;
        }

        if (!_vehicle.TryFindEntryPoint(vehicle, user, out var entryIndex))
            return false;

        if (!_vehicle.TryGetInteriorEntryCoordinates(vehicle, entryIndex, out var targetCoords))
            return false;

        if (!_hands.TryDrop(user, used, checkActionBlocker: false, doDropInteraction: false))
            return false;

        _transform.SetCoordinates(used, targetCoords);
        return true;
    }

    private EntityUid ResolveVehicleTarget(EntityUid target)
    {
        if (HasComp<VehicleEnterComponent>(target))
            return target;

        if (_vehicle.TryGetVehicleFromInterior(target, out var interiorVehicle) &&
            interiorVehicle is { } vehicle)
        {
            return vehicle;
        }

        return EntityUid.Invalid;
    }

    private bool IsPrimedHandGrenade(EntityUid entity)
    {
        return (_tag.HasTag(entity, HandGrenadeTag) || _tag.HasTag(entity, GrenadeTag)) &&
               HasComp<OnUseTimerTriggerComponent>(entity) &&
               HasComp<ActiveTimerTriggerComponent>(entity);
    }
}
