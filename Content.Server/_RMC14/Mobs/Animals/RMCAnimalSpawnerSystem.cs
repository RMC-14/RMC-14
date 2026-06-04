using Content.Shared._RMC14.Mobs.Animals;
using Content.Shared.Mobs.Components;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._RMC14.Mobs.Animals;

public sealed class RMCAnimalSpawnerSystem : RMCAnimalSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RMCAnimalSpawnerComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<RMCAnimalSpawnerComponent> ent, ref MapInitEvent args)
    {
        if (Random.Prob(ent.Comp.InitialChance))
            TrySpawnAnimal(ent);

        ent.Comp.NextLateSpawnAt = Timing.CurTime + RandomTime(ent.Comp.LateSpawnMin, ent.Comp.LateSpawnMax);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = Timing.CurTime;
        var query = EntityQueryEnumerator<RMCAnimalSpawnerComponent>();
        while (query.MoveNext(out var uid, out var spawner))
        {
            if (spawner.NextLateSpawnAt > now)
                continue;

            if (!XformQuery.TryGetComponent(uid, out var xform))
                continue;

            var mapId = Transform.GetMapCoordinates((uid, xform)).MapId;
            if (CountAlivePrototypeOnMap(spawner.Prototype, mapId) >= spawner.MaxAlive ||
                HasWitness(uid, spawner.WitnessRange))
            {
                spawner.NextLateSpawnAt = now + RandomTime(spawner.RetryMin, spawner.RetryMax);
                continue;
            }

            TrySpawnAnimal((uid, spawner));
            spawner.NextLateSpawnAt = now + RandomTime(spawner.RetryMin, spawner.RetryMax);
        }
    }

    private bool TrySpawnAnimal(Entity<RMCAnimalSpawnerComponent> ent)
    {
        if (!XformQuery.TryGetComponent(ent.Owner, out var xform))
            return false;

        var mapCoords = Transform.GetMapCoordinates((ent.Owner, xform));
        if (CountAlivePrototypeOnMap(ent.Comp.Prototype, mapCoords.MapId) >= ent.Comp.MaxAlive)
            return false;

        Spawn(ent.Comp.Prototype, xform.Coordinates);
        return true;
    }

    private int CountAlivePrototypeOnMap(EntProtoId prototype, MapId mapId)
    {
        var count = 0;
        var query = EntityQueryEnumerator<MobStateComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var mob, out var xform))
        {
            if (!MobState.IsAlive(uid, mob) ||
                MetaData(uid).EntityPrototype?.ID != prototype.Id)
            {
                continue;
            }

            var coords = Transform.GetMapCoordinates((uid, xform));
            if (coords.MapId == mapId)
                count++;
        }

        return count;
    }

    private bool HasWitness(EntityUid spawner, float range)
    {
        if (!XformQuery.TryGetComponent(spawner, out var spawnerXform))
            return false;

        var spawnerCoords = Transform.GetMapCoordinates((spawner, spawnerXform));
        var query = EntityQueryEnumerator<ActorComponent, MobStateComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out _, out var mob, out var xform))
        {
            if (!MobState.IsAlive(uid, mob))
                continue;

            var coords = Transform.GetMapCoordinates((uid, xform));
            if (coords.MapId == spawnerCoords.MapId &&
                (coords.Position - spawnerCoords.Position).Length() <= range)
            {
                return true;
            }
        }

        return false;
    }
}
