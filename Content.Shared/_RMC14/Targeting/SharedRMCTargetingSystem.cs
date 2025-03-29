using Content.Shared._RMC14.Inventory;
using Content.Shared._RMC14.Rangefinder;
using Content.Shared._RMC14.Rangefinder.Spotting;
using Content.Shared.Hands;
using Content.Shared.Interaction.Events;
using Robust.Shared.Map;
using Robust.Shared.Network;

namespace Content.Shared._RMC14.Targeting;

public abstract class SharedRMCTargetingSystem : EntitySystem
{
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
            StopTargeting(targeting, targeting.Comp.Targets[0]);
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
            StopTargeting(targeting, targeting.Comp.Targets[0]);
        }
    }

    /// <summary>
    ///     Remove the laser and replace targeting effects originating from the given entity.
    /// </summary>
    /// <param name="targetingUid">The entity that is targeting</param>
    /// <param name="target">The entity being targeted</param>
    /// <param name="targeting">The <see cref="TargetingComponent"/> belonging to the targing entity</param>
    public void StopTargeting(EntityUid targetingUid, EntityUid target, TargetingComponent? targeting = null)
    {
        if(!Resolve(targetingUid, ref targeting))
            return;

        if (!TryComp(target, out RMCTargetedComponent? targeted))
            return;

        targeted.TargetedBy.Remove(targetingUid);
        Dirty(target, targeted);

        targeting.Targets.Remove(target);
        Dirty(targetingUid, targeting);

        // Find the next target marker with the highest priority
        var highestMark = TargetedEffects.None;
        var highestDirection = DirectionTargetedEffects.None;
        var spotters = false;

        // Check all active lasers currently targeting the target
        foreach (var activeLaser in targeted.TargetedBy)
        {
            if (!TryComp(activeLaser, out TargetingComponent? comp))
                continue;

            if (comp.LaserType > highestMark)
                highestMark = comp.LaserType;

            if (comp.DirectionEffect > highestDirection)
                highestDirection = comp.DirectionEffect;

            if (comp.LaserType == TargetedEffects.Spotted)
                spotters = true;
        }

        // Remove the spotted component if none of the targeting lasers is a spotter
        if (!spotters)
            RemComp<SpottedComponent>(target);

        // Update the target marker
        UpdateTargetMarker(target, highestMark, highestDirection, true);

        if (targeted.TargetedBy.Count != 0)
            return;

        // Remove the target's targeted component if it has nothing targeting it anymore
        RemComp<RMCTargetedComponent>(target);

        if(targeting.Targets.Count == 0)
            RemComp<TargetingComponent>(targetingUid);
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
    /// <param name="directionEffect">The direction visualiser to apply on the entity being targeted</param>
    public void Target(EntityUid equipment, EntityUid user, EntityUid target, float targetingDuration, TargetedEffects targetedEffect = TargetedEffects.None, DirectionTargetedEffects directionEffect = DirectionTargetedEffects.None)
    {
        // Change the laser and targeting effect if focused.
        var ev = new TargetingStartedEvent(directionEffect, targetedEffect, target);
        RaiseLocalEvent(equipment, ref ev);
        targetedEffect = ev.TargetedEffect;
        directionEffect = ev.DirectionEffect;

        var targeted = EnsureComp<RMCTargetedComponent>(target);
        targeted.TargetedBy.Add(equipment);
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
        targeting.DirectionEffect = directionEffect;
        Dirty(equipment, targeting);

        var ev2 = new GotTargetedEvent();
        RaiseLocalEvent(target, ref ev2);

        UpdateTargetMarker(target, targetedEffect, directionEffect);
    }

    /// <summary>
    ///     Change the enabled visualiser on the target.
    /// </summary>
    /// <param name="target">The entity of which the visualiser should should change</param>
    /// <param name="newMarker">The new visualiser state</param>
    /// <param name="force">Should the new visualiser be applied, ignoring all conditions</param>
    /// <param name="directionEffect">The direction targeted visualiser to apply to the target</param>
    private void UpdateTargetMarker(EntityUid target, TargetedEffects newMarker,  DirectionTargetedEffects directionEffect, bool force = false)
    {
        //Get the currently active visualiser.
        _appearance.TryGetData<TargetedEffects>(target, TargetedVisuals.Targeted, out var marker);

        // Only apply the visualiser if forced, or it has a higher priority than the already existing one.
        if (force || newMarker > marker)
            _appearance.SetData(target, TargetedVisuals.Targeted, newMarker);

        var directionVisual = directionEffect > DirectionTargetedEffects.None;
        var directionVisualIntense = directionEffect > DirectionTargetedEffects.DirectionTargeted;

        _appearance.SetData(target, TargetedVisuals.TargetedDirection, directionVisual && !directionVisualIntense);
        _appearance.SetData(target, TargetedVisuals.TargetedDirectionIntense, directionVisual && directionVisualIntense);
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
                        var ev = new TargetingFinishedEvent(targeting.User, Transform(target).Coordinates, target);
                        RaiseLocalEvent(uid, ref ev);
                        break;
                    }
                    laserNumber++;
                }
                targetNumber++;
            }

            // Remove the active component and raise an event if the user moves.
            if (!_transform.InRange(Transform(xform.ParentUid).Coordinates, targeting.Origin, 0.1f))
            {
                var ev = new TargetingCancelledEvent();
                RaiseLocalEvent(uid, ref ev);

                targeting.LaserDurations.Clear();
                targeting.OriginalLaserDurations.Clear();
                Dirty(uid, targeting);
                while (targeting.Targets.Count > 0)
                {
                    StopTargeting(uid, targeting.Targets[0]);
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
public record struct TargetingStartedEvent(DirectionTargetedEffects DirectionEffect, TargetedEffects TargetedEffect, EntityUid Target);

/// <summary>
///     Raised on an entity when targeting has finished
/// </summary>
[ByRefEvent]
public record struct GotTargetedEvent();
