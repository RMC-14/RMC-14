using Content.Shared.Inventory;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Random;

namespace Content.Shared._RMC14.Gibbing;
public sealed class RMCGibSystem : EntitySystem
{
    private const float ItemLaunchImpulse = 8f;
    private const float ItemLaunchImpulseVariance = 3f;

    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    /// <summary>
    /// Scatters all inventory items from a target entity with physics impulses,
    /// similar to how body parts are launched during gibbing.
    /// </summary>
    /// <param name="target">The entity whose items should be scattered</param>
    /// <param name="launchImpulse">Base impulse force for launching items</param>
    /// <param name="launchImpulseVariance">Random variance added to the launch impulse</param>
    public void ScatterInventoryItems(EntityUid target, float? launchImpulse = null, float? launchImpulseVariance = null)
    {
        if (!TryComp<InventoryComponent>(target, out var inventory))
            return;

        var impulse = launchImpulse ?? ItemLaunchImpulse;
        var impulseVariance = launchImpulseVariance ?? ItemLaunchImpulseVariance;

        var targetTransform = Transform(target);
        foreach (var item in _inventory.GetHandOrInventoryEntities(target))
        {
            // Drop the item next to the target
            _transform.DropNextTo(item, (target, targetTransform));

            // Apply random launch impulse to scatter the item
            var scatterAngle = _random.NextAngle();
            var scatterVector = scatterAngle.ToVec() * (impulse + _random.NextFloat(impulseVariance));
            _physics.ApplyLinearImpulse(item, scatterVector);

            // Give the item a random rotation for visual effect
            _transform.SetWorldRotation(item, _random.NextAngle());
        }
    }
}
