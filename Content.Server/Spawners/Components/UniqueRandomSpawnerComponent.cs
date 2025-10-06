using Robust.Shared.Prototypes;

namespace Content.Server.Spawners.Components
{
    /// <summary>
    /// A spawner that randomly selects from a list of prototypes without spawning duplicates.
    /// All spawners with the same group will share a pool and avoid duplicates.
    /// Inherits the Prototypes field (List&lt;EntProtoId&gt;) from ConditionalSpawnerComponent.
    /// </summary>
    [RegisterComponent, EntityCategory("Spawner")]
    public sealed partial class UniqueRandomSpawnerComponent : ConditionalSpawnerComponent
    {
        /// <summary>
        /// A unique identifier for this group of spawners.
        /// All spawners with the same group will share the same pool of available prototypes.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField(required: true)]
        public EntProtoId SpawnerGroup { get; set; } = default;

        /// <summary>
        /// Whether to delete the spawner after spawning
        /// </summary>
        [DataField]
        public bool DeleteSpawnerAfterSpawn = true;
    }
}
