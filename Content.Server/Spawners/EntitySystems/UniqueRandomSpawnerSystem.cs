
using System.Numerics;
using Content.Server.GameTicking;
using Content.Server.Spawners.Components;
using Content.Shared.GameTicking;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Spawners.EntitySystems
{
    /// <summary>
    /// System that manages unique random spawners.
    /// Ensures no duplicate prototypes are spawned within the same spawner group.
    /// </summary>
    public sealed class UniqueRandomSpawnerSystem : EntitySystem
    {
        [Dependency] private readonly IRobustRandom _robustRandom = default!;

        /// <summary>
        /// Tracks the remaining prototypes for each spawner group.
        /// Key: spawner group identifier
        /// Value: list of prototypes that haven't been spawned yet
        /// </summary>
        private Dictionary<EntProtoId, List<EntProtoId>> _remainingPrototypes = new();

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<UniqueRandomSpawnerComponent, MapInitEvent>(OnMapInit);
            SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundCleanup);
        }

        private void OnRoundCleanup(RoundRestartCleanupEvent ev)
        {
            // Clear all pools at round end so they reset for the next round
            _remainingPrototypes.Clear();
        }

        private void OnMapInit(Entity<UniqueRandomSpawnerComponent> ent, ref MapInitEvent args)
        {
            Spawn(ent);
            
            if (ent.Comp.DeleteSpawnerAfterSpawn)
                QueueDel(ent);
        }

        private void Spawn(Entity<UniqueRandomSpawnerComponent> ent)
        {
            if (ent.Comp.Prototypes.Count == 0)
            {
                Log.Warning($"Prototype list in UniqueRandomSpawnerComponent is empty! Entity: {ToPrettyString(ent)}");
                return;
            }
            if (Deleted(ent))
                return;
            // Initialize the pool for this group if it doesn't exist
            if (!_remainingPrototypes.ContainsKey(ent.Comp.SpawnerGroup))
            {
                _remainingPrototypes[ent.Comp.SpawnerGroup] = new List<EntProtoId>(ent.Comp.Prototypes);
            }
            var pool = _remainingPrototypes[ent.Comp.SpawnerGroup];
            
            if (pool.Count == 0)
            {
                Log.Warning($"No more unique prototypes available for group {ent.Comp.SpawnerGroup}. Resetting pool. Entity: {ToPrettyString(ent)}");
                pool.AddRange(ent.Comp.Prototypes);
            }
            // Pick a random prototype from the remaining pool
            var selectedProto = _robustRandom.Pick(pool);
            
            // Remove it from the pool so it won't be picked again
            pool.Remove(selectedProto);
            // Spawn the entity
            Spawn(selectedProto, Transform(ent).Coordinates);
        }
    }
}