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

public abstract class RMCAnimalSystem : EntitySystem
{
    [Dependency] protected readonly SharedContainerSystem Container = default!;
    [Dependency] protected readonly DamageableSystem Damageable = default!;
    [Dependency] protected readonly EntityLookupSystem Lookup = default!;
    [Dependency] protected readonly MobStateSystem MobState = default!;
    [Dependency] protected readonly NpcFactionSystem Faction = default!;
    [Dependency] protected readonly SharedPhysicsSystem Physics = default!;
    [Dependency] protected readonly SharedPopupSystem Popup = default!;
    [Dependency] protected readonly RMCNPCSystem RMCNpc = default!;
    [Dependency] protected readonly IRobustRandom Random = default!;
    [Dependency] protected readonly SharedStunSystem Stun = default!;
    [Dependency] protected readonly TagSystem Tags = default!;
    [Dependency] protected readonly IGameTiming Timing = default!;
    [Dependency] protected readonly SharedTransformSystem Transform = default!;

    protected EntityQuery<ActorComponent> ActorQuery;
    protected EntityQuery<DamageableComponent> DamageableQuery;
    protected EntityQuery<FlammableComponent> FlammableQuery;
    protected EntityQuery<ItemComponent> ItemQuery;
    protected EntityQuery<MobStateComponent> MobQuery;
    protected EntityQuery<MobThresholdsComponent> ThresholdsQuery;
    protected EntityQuery<NpcFactionMemberComponent> FactionQuery;
    protected EntityQuery<PhysicsComponent> PhysicsQuery;
    protected EntityQuery<TransformComponent> XformQuery;

    public override void Initialize()
    {
        base.Initialize();

        ActorQuery = GetEntityQuery<ActorComponent>();
        DamageableQuery = GetEntityQuery<DamageableComponent>();
        FlammableQuery = GetEntityQuery<FlammableComponent>();
        ItemQuery = GetEntityQuery<ItemComponent>();
        MobQuery = GetEntityQuery<MobStateComponent>();
        ThresholdsQuery = GetEntityQuery<MobThresholdsComponent>();
        FactionQuery = GetEntityQuery<NpcFactionMemberComponent>();
        PhysicsQuery = GetEntityQuery<PhysicsComponent>();
        XformQuery = GetEntityQuery<TransformComponent>();
    }

    protected TimeSpan RandomTime(TimeSpan min, TimeSpan max)
    {
        if (max <= min)
            return min;

        return min + TimeSpan.FromSeconds(Random.NextDouble() * (max - min).TotalSeconds);
    }

    protected bool ValidLivingMob(EntityUid uid)
    {
        return !TerminatingOrDeleted(uid) &&
               MobQuery.HasComp(uid) &&
               MobState.IsAlive(uid);
    }

    protected bool IsOnFire(EntityUid uid)
    {
        return FlammableQuery.TryComp(uid, out var flammable) && flammable.OnFire;
    }

    protected void StopMovement(EntityUid uid)
    {
        if (PhysicsQuery.TryComp(uid, out var physics))
            Physics.SetLinearVelocity(uid, Vector2.Zero, body: physics);
    }

    protected bool TryMoveTowards(EntityUid uid, EntityCoordinates target, float speed)
    {
        if (!PhysicsQuery.TryComp(uid, out var physics))
            return false;

        var origin = Transform.GetMoverCoordinates(uid);
        if (!origin.TryDistance(EntityManager, target, out _))
            return false;

        var direction = target.Position - origin.Position;
        if (direction.LengthSquared() < 0.01f)
            return false;

        Physics.SetLinearVelocity(uid, direction.Normalized() * speed, body: physics);
        return true;
    }

    protected bool TryMoveAwayFrom(EntityUid uid, EntityCoordinates target, float speed)
    {
        if (!PhysicsQuery.TryComp(uid, out var physics))
            return false;

        var origin = Transform.GetMoverCoordinates(uid);
        if (!origin.TryDistance(EntityManager, target, out _))
            return false;

        var direction = origin.Position - target.Position;
        if (direction.LengthSquared() < 0.01f)
            direction = Random.NextAngle().RotateVec(Vector2.UnitX);

        Physics.SetLinearVelocity(uid, direction.Normalized() * speed, body: physics);
        return true;
    }

    protected bool TryMoveRandomly(EntityUid uid, float speed)
    {
        if (!PhysicsQuery.TryComp(uid, out var physics))
            return false;

        var direction = Random.NextAngle().RotateVec(Vector2.UnitX);
        Physics.SetLinearVelocity(uid, direction * speed, body: physics);
        return true;
    }

    protected int CountNearby<T>(MapCoordinates coordinates, float range) where T : IComponent
    {
        var count = 0;
        foreach (var ent in Lookup.GetEntitiesInRange<T>(coordinates, range))
        {
            if (ent.Owner.Valid)
                count++;
        }

        return count;
    }

    protected void SpawnNear(EntProtoId prototype, EntityCoordinates coordinates, float radius)
    {
        var offset = Random.NextAngle().RotateVec(Vector2.UnitX) * Random.NextFloat(0f, radius);
        Spawn(prototype, coordinates.Offset(offset));
    }
}
