using Content.Shared.Coordinates;
using Content.Shared.Throwing;
using Robust.Shared.Map;
using Robust.Shared.Random;

namespace Content.Shared._RMC14.Explosion;

public abstract class SharedRMCExplosionSystem : EntitySystem
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
        DoEffect(ent);
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

    public void DoEffect(Entity<CMExplosionEffectComponent> ent)
    {
        if (ent.Comp.ShockWave is { } shockwave)
            SpawnNextToOrDrop(shockwave, ent);

        if (ent.Comp.Explosion is { } explosion)
            SpawnNextToOrDrop(explosion, ent);

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

    public void TryDoEffect(Entity<CMExplosionEffectComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        DoEffect((ent, ent.Comp));
    }

    public virtual void QueueExplosion(
        MapCoordinates epicenter,
        string typeId,
        float totalIntensity,
        float slope,
        float maxTileIntensity,
        EntityUid? cause,
        float tileBreakScale = 1f,
        int maxTileBreak = int.MaxValue,
        bool canCreateVacuum = true,
        bool addLog = true)
    {
    }

    public virtual void TriggerExplosive(
        EntityUid uid,
        bool delete = true,
        float? totalIntensity = null,
        float? radius = null,
        EntityUid? user = null)
    {
    }
}
