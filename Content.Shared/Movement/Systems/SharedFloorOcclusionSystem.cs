using Content.Shared._RMC14.Water;
using Content.Shared.Movement.Components;

namespace Content.Shared.Movement.Systems;

/// <summary>
/// Applies an occlusion shader for any relevant entities.
/// </summary>
public abstract class SharedFloorOcclusionSystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly RMCWaterSystem _rmcWater = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private readonly HashSet<Entity<FloorOccluderComponent>> _nearbyOccluders = new();
    private readonly HashSet<EntityUid> _validOccluders = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<FloorOcclusionComponent, MapInitEvent>(OnOcclusionMapInit);
    }

    private void OnOcclusionMapInit(Entity<FloorOcclusionComponent> ent, ref MapInitEvent args)
    {
        SyncOcclusion(ent, ent.Comp);
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<FloorOcclusionComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var occlusion, out var xform))
        {
            if (xform.MapUid == null)
                continue;

            SyncOcclusion(uid, occlusion);
        }
    }

    private void SyncOcclusion(EntityUid uid, FloorOcclusionComponent occlusion)
    {
        var coords = _transform.GetMoverCoordinates(uid);
        _nearbyOccluders.Clear();
        _lookup.GetEntitiesInRange(coords, 0.5f, _nearbyOccluders);

        _validOccluders.Clear();
        foreach (var occluder in _nearbyOccluders)
        {
            if (_rmcWater.CanCollide(occluder.Owner, uid))
                _validOccluders.Add(occluder.Owner);
        }

        var changed = false;

        // Remove stale or out-of-range entries
        for (var i = occlusion.Colliding.Count - 1; i >= 0; i--)
        {
            if (!_validOccluders.Contains(occlusion.Colliding[i]))
            {
                occlusion.Colliding.RemoveAt(i);
                changed = true;
            }
        }

        // Add nearby occluders not already tracked
        foreach (var valid in _validOccluders)
        {
            if (!occlusion.Colliding.Contains(valid))
            {
                occlusion.Colliding.Add(valid);
                changed = true;
            }
        }

        if (changed)
        {
            Dirty(uid, occlusion);
            SetEnabled((uid, occlusion));
        }
    }

    protected virtual void SetEnabled(Entity<FloorOcclusionComponent> entity)
    {

    }
}
