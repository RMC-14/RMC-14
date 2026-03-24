using Content.Shared._RMC14.Stun;
using Content.Shared.Ghost;
using Robust.Shared.Map;

namespace Content.Shared._RMC14.Explosion.Implosion;

public sealed class RMCImplosionSystem : EntitySystem
{
    [Dependency] private readonly RMCSizeStunSystem _sizeStun = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;

    private EntityQuery<GhostComponent> _ghostQuery;

    public override void Initialize()
    {
        base.Initialize();
        _ghostQuery = GetEntityQuery<GhostComponent>();
    }

    public void Implode(RMCImplosion implosion, MapCoordinates origin)
    {
        var nearbyEntities = _entityLookup.GetEntitiesInRange(origin, implosion.PullRange, LookupFlags.Uncontained);
        foreach (var entity in nearbyEntities)
        {
            if (_ghostQuery.HasComp(entity))
                continue;

            _sizeStun.KnockBack(entity, origin, -implosion.PullDistance, -implosion.PullDistance, implosion.PullSpeed, implosion.IgnoreSize);
        }
    }
}
