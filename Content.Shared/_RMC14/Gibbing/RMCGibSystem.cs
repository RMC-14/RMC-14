using Content.Shared.Body.Events;
using Content.Shared.Body.Systems;
using Content.Shared.Damage;
using Content.Shared.Inventory;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Network;
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
    [Dependency] private readonly SharedBodySystem _body = default!;
    [Dependency] private readonly MobThresholdSystem _thresholds = default!;
    [Dependency] private readonly INetManager _net = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RMCSpawnEntitiesOnGibComponent, BeingGibbedEvent>(OnGibbed);
        SubscribeLocalEvent<RMCGibOnDeathComponent, MobStateChangedEvent>(OnDeath);
    }

    private void OnGibbed(Entity<RMCSpawnEntitiesOnGibComponent> ent, ref BeingGibbedEvent args)
    {
        if (_net.IsClient)
            return;

        foreach (var protoId in ent.Comp.Entities)
        {
            var position = _transform.GetMoverCoordinates(ent);
            var newEntity = Spawn(protoId, position);
            _transform.AttachToGridOrMap(newEntity);
        }
    }

    private void OnDeath(Entity<RMCGibOnDeathComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Dead)
            return;

        var gibProbability = ent.Comp.GibChance;

        if (TryComp<MobThresholdsComponent>(ent, out var thresholds) && TryComp<DamageableComponent>(ent, out var damageable))
        {
            var damage = damageable.Damage.GetTotal();
            var dead = _thresholds.GetThresholdForState(ent, MobState.Dead, thresholds);
            gibProbability += (float)(damage - dead) * ent.Comp.DamageGibMultiplier;
        }

        if (_random.NextFloat() > gibProbability)
            return;

        if (_net.IsClient)
            return;

        _body.GibBody(ent, ent.Comp.DropOrgans);
    }

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
