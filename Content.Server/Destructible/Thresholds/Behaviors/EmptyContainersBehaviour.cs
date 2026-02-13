using Robust.Server.Containers;
using Robust.Shared.Containers;
using Robust.Shared.Random;

namespace Content.Server.Destructible.Thresholds.Behaviors
{
    /// <summary>
    ///     Drop all items from specified containers
    /// </summary>
    [DataDefinition]
    public sealed partial class EmptyContainersBehaviour : IThresholdBehavior //
    {
        [DataField("containers")]
        public List<string> Containers = new();

        // RMC14
        [DataField]
        public float DeleteChance;
        //

        public void Execute(EntityUid owner, DestructibleSystem system, EntityUid? cause = null)
        {
            if (!system.EntityManager.TryGetComponent<ContainerManagerComponent>(owner, out var containerManager))
                return;

            var containerSys = system.EntityManager.System<ContainerSystem>();

            //RMC14
            var random = IoCManager.Resolve<IRobustRandom>();
            //

            foreach (var containerId in Containers)
            {
                if (!containerSys.TryGetContainer(owner, containerId, out var container, containerManager))
                    continue;

                // RMC14
                if (random.NextFloat() < DeleteChance)
                {
                    foreach (var entity in container.ContainedEntities)
                    {
                        system.EntityManager.DeleteEntity(entity);
                    }
                }
                //

                containerSys.EmptyContainer(container, true);
            }
        }
    }
}
