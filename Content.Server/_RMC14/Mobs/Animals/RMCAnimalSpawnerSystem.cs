using System.Linq;
using System.Numerics;
using Content.Server._RMC14.Atmos;
using Content.Server._RMC14.Barricade;
using Content.Server._RMC14.NPC;
using Content.Shared._RMC14.Barricade;
using Content.Shared._RMC14.CameraShake;
using Content.Shared._RMC14.Mobs.Animals;
using Content.Shared._RMC14.Slow;
using Content.Shared._RMC14.Stun;
using Content.Shared._RMC14.Vents;
using Content.Shared._RMC14.Xenonids;
using Content.Shared._RMC14.Xenonids.Damage;
using Content.Shared._RMC14.Xenonids.Leap;
using Content.Shared.Actions;
using Content.Shared.Atmos.Components;
using Content.Shared.CombatMode;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Hands;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Item;
using Content.Shared.Maps;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.NPC.Components;
using Content.Shared.NPC.Systems;
using Content.Shared.Nutrition.Components;
using Content.Shared.Physics;
using Content.Shared.Placeable;
using Content.Shared.Popups;
using Content.Shared.Projectiles;
using Content.Shared.Spider;
using Content.Shared.Standing;
using Content.Shared.Stunnable;
using Content.Shared.Tag;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Whitelist;
using Robust.Shared.Containers;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

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

            if (CountAliveSpawned(spawner) >= spawner.MaxAlive || HasWitness(uid, spawner.WitnessRange))
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
        if (CountAliveSpawned(ent.Comp) >= ent.Comp.MaxAlive)
            return false;

        if (!XformQuery.TryGetComponent(ent.Owner, out var xform))
            return false;

        var spawned = Spawn(ent.Comp.Prototype, xform.Coordinates);
        ent.Comp.Spawned.Add(spawned);
        return true;
    }

    private int CountAliveSpawned(RMCAnimalSpawnerComponent spawner)
    {
        var count = 0;
        for (var i = spawner.Spawned.Count - 1; i >= 0; i--)
        {
            var spawned = spawner.Spawned[i];
            if (TerminatingOrDeleted(spawned))
            {
                spawner.Spawned.RemoveAt(i);
                continue;
            }

            if (ValidLivingMob(spawned))
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
