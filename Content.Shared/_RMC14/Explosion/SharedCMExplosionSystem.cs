using Content.Shared.Coordinates;
using Content.Shared.Throwing;
using Robust.Shared.Random;

namespace Content.Shared._RMC14.Explosion;

public abstract class SharedCMExplosionSystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;

    private readonly HashSet<Entity<RMCWallExplosionDeletableComponent>> _walls = new();

    public override void Initialize()
    {
        SubscribeLocalEvent<CMExplosionEffectComponent, CMExplosiveTriggeredEvent>(OnExplosionEffectTriggered);

        SubscribeLocalEvent<RMCExplosiveDeleteWallsComponent, CMExplosiveTriggeredEvent>(OnDeleteWallsTriggered);
    }

    private void OnExplosionEffectTriggered(Entity<CMExplosionEffectComponent> ent, ref CMExplosiveTriggeredEvent args)
    {
        SpawnNextToOrDrop(ent.Comp.ShockWave, ent);
        SpawnNextToOrDrop(ent.Comp.Explosion, ent);

        if (ent.Comp.MaxShrapnel > 0)
        {
            foreach (var effect in ent.Comp.ShrapnelEffects)
            {
                var shrapnelCount = _random.Next(ent.Comp.MinShrapnel, ent.Comp.MaxShrapnel);
                for (var i = 0; i < shrapnelCount; i++)
                {
                    var angle = _random.NextAngle();
                    var direction = angle.ToVec().Normalized() * 10;
                    var shrapnel = SpawnNextToOrDrop(effect, ent);
                    _throwing.TryThrow(shrapnel, direction, ent.Comp.ShrapnelSpeed / 10);
                }
            }
        }
    }

    private void OnDeleteWallsTriggered(Entity<RMCExplosiveDeleteWallsComponent> ent, ref CMExplosiveTriggeredEvent args)
    {
        _walls.Clear();
        _entityLookup.GetEntitiesInRange(ent.Owner.ToCoordinates(), ent.Comp.Range, _walls);

        foreach (var wall in _walls)
        {
            QueueDel(wall);
        }
    }
}
