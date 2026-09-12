using System;
using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.Physics;

namespace Content.Shared._RMC14.Vehicle;

public sealed partial class GridVehicleMoverSystem
{
    private const float ToxicWaterAcidResistantMultiplier = 0.5f;
    private static readonly TimeSpan ToxicWaterDamageEvery = TimeSpan.FromSeconds(1);

    private readonly Dictionary<EntityUid, TimeSpan> _nextToxicWaterDamage = new();
    private readonly HashSet<Entity<VehicleCorrosiveTileComponent>> _corrosiveTiles = new();

    private void UpdateVehicleToxicWater(EntityUid uid, GridVehicleMoverComponent mover, TransformComponent xform)
    {
        if (_net.IsClient)
            return;

        if (mover.WeightClass != VehicleWeightClass.Weak)
            return;

        if (!HasComp<VehicleWheelSlotsComponent>(uid))
            return;

        var now = _timing.CurTime;
        if (_nextToxicWaterDamage.TryGetValue(uid, out var next) && now < next)
            return;

        if (!fixtureQ.TryComp(uid, out var fixtures))
            return;

        var tx = _physics.GetPhysicsTransform(uid, xform);
        if (!TryGetFixtureAabb(fixtures, tx, out var aabb))
            return;

        _corrosiveTiles.Clear();
        _lookup.GetEntitiesIntersecting(xform.MapID, aabb, _corrosiveTiles);

        var totalDamage = 0f;
        foreach (var tile in _corrosiveTiles)
        {
            totalDamage += tile.Comp.WheelDamage;
        }

        if (totalDamage <= 0f)
        {
            _nextToxicWaterDamage.Remove(uid);
            return;
        }

        _nextToxicWaterDamage[uid] = now + ToxicWaterDamageEvery;
        _wheels.DamageWheelsCorrosive(uid, totalDamage, ToxicWaterAcidResistantMultiplier);
    }
}
