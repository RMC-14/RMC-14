using Content.Shared._RMC14.Stun;
using Robust.Shared.Map;

namespace Content.Shared._RMC14.Explosion.Implosion;

public sealed class RMCImplosionSystem : EntitySystem
{
    [Dependency] private readonly RMCSizeStunSystem _sizeStun = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;

    public void Implode(RMCImplosion implosion, MapCoordinates origin)
    {
        var nearbyEntities = _entityLookup.GetEntitiesInRange(origin, implosion.PullRange, LookupFlags.Uncontained);
        foreach (var entity in nearbyEntities)
        {
            _sizeStun.KnockBack(entity, origin, -implosion.PullDistance, -implosion.PullDistance, implosion.PullSpeed, implosion.IgnoreSize);
        }
    }
}
