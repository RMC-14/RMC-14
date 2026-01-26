using Content.Shared._RMC14.Inventory;
using Content.Shared._RMC14.Rangefinder;
using Content.Shared._RMC14.Rangefinder.Spotting;
using Content.Shared.Hands;
using Content.Shared.Interaction.Events;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Targeting;

public abstract class SharedRMCTargetingSystem : EntitySystem
{
    [Dependency] protected readonly IGameTiming Timing = default!;

    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<TargetingComponent, ComponentRemove>(OnTargetingRemoved);
        SubscribeLocalEvent<TargetingComponent, DroppedEvent>(OnTargetingDropped);
        SubscribeLocalEvent<TargetingComponent, RMCDroppedEvent>(OnTargetingDropped);
        SubscribeLocalEvent<TargetingComponent, GotUnequippedHandEvent>(OnTargetingDropped);
        SubscribeLocalEvent<TargetingComponent, HandDeselectedEvent>(OnTargetingDropped);

        SubscribeLocalEvent<RMCTargetedComponent, ComponentRemove>(OnTargetedRemove);
        SubscribeLocalEvent<RMCTargetedComponent, EntityTerminatingEvent>(OnTargetedRemove);
    }

    /// <summary>
    ///     Remove any lasers if the <see cref="TargetingComponent"/> is being removed from an entity.
    /// </summary>
    private void OnTargetingRemoved<T>(Entity<TargetingComponent> targeting, ref T args)
    {
        if (_net.IsClient)
            return;

        if(TryComp(targeting, out RangefinderComponent? rangefinder))
            _appearance.SetData(targeting, RangefinderLayers.Layer, rangefinder.Mode);

        while (targeting.Comp.Targets.Count > 0)
        {
            StopTargeting((targeting, targeting), targeting.Comp.Targets[0]);
        }
    }

    /// <summary>
    ///     Stop all targeting done by the entity if it's interrupted.
    /// </summary>
    private void OnTargetingDropped<T>(Entity<TargetingComponent> targeting, ref T args)
    {
        var ev = new TargetingCancelledEvent();
        RaiseLocalEvent(targeting, ref ev);

        while (targeting.Comp.Targets.Count > 0)
        {
            StopTargeting((targeting, targeting), targeting.Comp.Targets[0]);
        }
    }

    private void OnTargetedRemove<T>(Entity<RMCTargetedComponent> ent, ref T args)
    {
        foreach (var targeting in ent.Comp.TargetedBy)
        {
            if (!TryComp(targeting, out TargetingComponent? targetingComp))
                continue;

            targetingComp.Targets.Remove(ent);
            targetingComp.LaserDurations.Remove(ent);
            targetingComp.OriginalLaserDurations.Remove(ent);
            Dirty(targeting, targetingComp);
        }
    }

    /// <summary>
    ///     Remove the laser and replace targeting effects originating from the given entity.
    /// </summary>
    /// <param name="targeting">The entity that is targeting</param>
    /// <param name="target">The entity being targeted</param>
    public void StopTargeting(Entity<TargetingComponent?> targeting, EntityUid target)
    {
        if (!Resolve(targeting, ref targeting.Comp, false))
            return;

        targeting.Comp.Targets.Remove(target);
        Dirty(targeting);

        if (!TryComp(target, out RMCTargetedComponent? targeted))
            return;

        targeted.TargetedBy.Remove(targeting);
        Dirty(target, targeted);

        // Find the next target marker with the highest priority
        var highestMark = TargetedEffects.None;
        var spotters = false;

        // Check all active lasers currently targeting the target
        foreach (var activeLaser in targeted.TargetedBy)
        {
            if (!TryComp(activeLaser, out TargetingComponent? comp))
                continue;

            if (comp.LaserType > highestMark)
                highestMark = comp.LaserType;

            if (comp.LaserType == TargetedEffects.Spotted)
                spotters = true;
        }

        targeted.TargetType = highestMark;
        Dirty(target, targeted);

        // Remove the spotted component if none of the targeting lasers is a spotter
        if (!spotters)
            RemComp<SpottedComponent>(target);

        if (targeted.TargetedBy.Count != 0)
            return;

        // Remove the target's targeted component if it has nothing targeting it anymore
        RemComp<RMCTargetedComponent>(target);

        if (targeting.Comp.Targets.Count == 0)
            RemComp<TargetingComponent>(targeting);
    }

    /// <summary>
    ///     Apply the <see cref="TargetingComponent"/> to the entity creating the laser.
    ///     Apply the <see cref="RMCTargetedComponent"/> to the entity being targeted.
    /// </summary>
    /// <param name="equipment">The entity that is targeting</param>
    /// <param name="user">The user of the targeting entity</param>
    /// <param name="target">The entity being targeted</param>
    /// <param name="targetingDuration">How long the targeting should last if not interrupted</param>
    /// <param name="targetedEffect">The visualiser to apply on the entity being targeted</param>
    /// <param name="showDirection">If a direction indicator pointing towards the targeting entity should be displayed</param>
    public void Target(EntityUid equipment, EntityUid user, EntityUid target, float targetingDuration, TargetedEffects targetedEffect = TargetedEffects.None, bool showDirection = false)
    {
        // Change the laser and targeting effect if focused.
        var ev = new TargetingStartedEvent(targetedEffect, target);
        RaiseLocalEvent(equipment, ref ev);
        targetedEffect = ev.TargetedEffect;

        var targeted = EnsureComp<RMCTargetedComponent>(target);
        targeted.TargetedBy.Add(equipment);
        targeted.ShowDirection = showDirection;
        if (targetedEffect > targeted.TargetType)
            targeted.TargetType = targetedEffect;
        Dirty(target, targeted);

        var targeting = EnsureComp<TargetingComponent>(equipment);
        if (!targeting.LaserDurations.TryAdd(target, new List<float>{targetingDuration}))
            targeting.LaserDurations[target].Add(targetingDuration);

        if (!targeting.OriginalLaserDurations.TryAdd(target, new List<float>{targetingDuration}))
            targeting.OriginalLaserDurations[target].Add(targetingDuration);

        targeting.Source = equipment;
        targeting.Targets.Add(target);
        targeting.Origin = Transform(user).Coordinates;
        targeting.User = user;
        targeting.LaserType = targetedEffect;
        Dirty(equipment, targeting);
    }

    /// <summary>
    ///     Remove a laser pointing to a specific target,
    /// </summary>
    /// <param name="ent">The targeting entity</param>
    /// <param name="target">The target the laser is pointing to</param>
    /// <param name="laserNumber">The position of the laser in the list of lasers connecting to the target</param>
    private void RemoveLaser(Entity<TargetingComponent> ent, EntityUid target, int laserNumber)
    {
        ent.Comp.LaserDurations[target].RemoveAt(laserNumber);
        ent.Comp.OriginalLaserDurations[target].RemoveAt(laserNumber);
        if (ent.Comp.LaserDurations[target].Count <= 0)
        {
            ent.Comp.LaserDurations.Remove(target);
            ent.Comp.OriginalLaserDurations.Remove(target);
        }

        Dirty(ent);
    }

    /// <summary>
    ///     Reduce the duration of targeting effects every frame.
    /// </summary>
    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<TargetingComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var targeting, out var xform))
        {
            var targetNumber = 0;
            var checkedTargets = new List<EntityUid>();
            while (targetNumber < targeting.Targets.Count)
            {
                var target = targeting.Targets[targetNumber];
                if (!targeting.LaserDurations.Keys.Contains(target) || checkedTargets.Contains(target))
                {
                    targetNumber++;
                    continue;
                }

                checkedTargets.Add(target);

                var laserNumber = 0;
                while (laserNumber < targeting.LaserDurations[target].Count)
                {
                    targeting.LaserDurations[target][laserNumber] -= frameTime;
                    Dirty(uid, targeting);

                    // Adjust alpha of the laser based on how close it is to finishing targeting.
                    if (TryComp(target, out RMCTargetedComponent? targeted))
                    {
                        var newAlpha = 1 - targeting.LaserDurations[target][laserNumber] / targeting.OriginalLaserDurations[target][laserNumber];
                        targeted.AlphaMultipliers.TryAdd(uid, newAlpha);
                        if (newAlpha > targeted.AlphaMultipliers[uid])
                            targeted.AlphaMultipliers[uid] =  newAlpha;
                    }

                    // Raise an event and stop targeting if the targeting successfully finishes.
                    if (targeting.LaserDurations[target][laserNumber] <= 0)
                    {
                        RemoveLaser((uid, targeting), target, laserNumber);
                        if (TryComp(target, out TransformComponent? targetTransform))
                        {
                            var ev = new TargetingFinishedEvent(targeting.User, targetTransform.Coordinates, target);
                            RaiseLocalEvent(uid, ref ev);
                        }
                        break;
                    }
                    laserNumber++;
                }
                targetNumber++;
            }

            // Remove the active component and raise an event if the user moves.
            if (TryComp(xform.ParentUid, out TransformComponent? parentTransform) &&
                !_transform.InRange(parentTransform.Coordinates, targeting.Origin, 0.1f))
            {
                var ev = new TargetingCancelledEvent();
                RaiseLocalEvent(uid, ref ev);

                targeting.LaserDurations.Clear();
                targeting.OriginalLaserDurations.Clear();
                Dirty(uid, targeting);
                while (targeting.Targets.Count > 0)
                {
                    StopTargeting((uid, targeting), targeting.Targets[0]);
                }
            }
        }
    }
}

/// <summary>
///     Raised on an entity when it finishes targeting.
/// </summary>
[ByRefEvent]
public record struct TargetingFinishedEvent(EntityUid User, EntityCoordinates Coordinates, EntityUid Target, bool Handled = false);

/// <summary>
///     Raised on an entity when it's targeting is interrupted.
/// </summary>
[ByRefEvent]
public record struct TargetingCancelledEvent(bool Handled = false);

/// <summary>
///     Raised on an entity when it starts targeting.
/// </summary>
[ByRefEvent]
public record struct TargetingStartedEvent(TargetedEffects TargetedEffect, EntityUid Target);
