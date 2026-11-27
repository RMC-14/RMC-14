using System.Diagnostics.CodeAnalysis;
using Content.Shared.Physics;
using Robust.Shared.Map;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Physics.Systems;

namespace Content.Shared.Spawning
{
    public static class EntitySystemExtensions
    {
        public static EntityUid? SpawnIfUnobstructed(
            this IEntityManager entityManager,
            string? prototypeName,
            EntityCoordinates coordinates,
            CollisionGroup collisionLayer,
            in Box2? box = null,
            EntityLookupSystem? lookup = null)
        {
            var mapCoordinates = entityManager.System<SharedTransformSystem>().ToMapCoordinates(coordinates);

            return entityManager.SpawnIfUnobstructed(prototypeName, mapCoordinates, collisionLayer, box, lookup);
        }

        public static EntityUid? SpawnIfUnobstructed(
            this IEntityManager entityManager,
            string? prototypeName,
            MapCoordinates coordinates,
            CollisionGroup collisionLayer,
            in Box2? box = null,
            EntityLookupSystem? lookup = null)
        {
            var boxOrDefault = box.GetValueOrDefault(Box2.UnitCentered).Translated(coordinates.Position);
            lookup ??= entityManager.System<EntityLookupSystem>();

            // RMC14
            var query = new FixtureQueryArgs(
                new QueryFilter
                {
                    LayerBits = -1L,
                    MaskBits = -1L,
                    Flags = QueryFlags.Dynamic | QueryFlags.Static | QueryFlags.Sensors,
                },
                Approximate: true);

            if (!lookup.ForEachFixtureIntersecting(coordinates.MapId, boxOrDefault, ref collisionLayer,
                    new SpawnCollisionCallback(), query))
            {
                return null;
            }
            // RMC14

            return entityManager.Spawn(prototypeName, coordinates);
        }

        public static bool TrySpawnIfUnobstructed(
            this IEntityManager entityManager,
            string? prototypeName,
            EntityCoordinates coordinates,
            CollisionGroup collisionLayer,
            [NotNullWhen(true)] out EntityUid? entity,
            Box2? box = null,
            EntityLookupSystem? lookup = null)
        {
            entity = entityManager.SpawnIfUnobstructed(prototypeName, coordinates, collisionLayer, box, lookup);

            return entity != null;
        }

        public static bool TrySpawnIfUnobstructed(
            this IEntityManager entityManager,
            string? prototypeName,
            MapCoordinates coordinates,
            CollisionGroup collisionLayer,
            [NotNullWhen(true)] out EntityUid? entity,
            in Box2? box = null,
            EntityLookupSystem? lookup = null)
        {
            entity = entityManager.SpawnIfUnobstructed(prototypeName, coordinates, collisionLayer, box, lookup);

            return entity != null;
        }

        // RMC14
        private readonly struct SpawnCollisionCallback : IFixtureQueryCallback<CollisionGroup>
        {
            public bool Invoke(ref CollisionGroup collisionLayer, in FixtureProxy fixture)
            {
                var body = fixture.Body;
                if (!body.Hard)
                    return true;

                // TODO: wtf fix this
                return collisionLayer == 0 || (body.CollisionMask & (int) collisionLayer) == 0;
            }
        }
        // RMC14
    }
}
