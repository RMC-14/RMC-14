using Content.Shared._RMC14.Inventory;
using Content.Shared._RMC14.Rangefinder.Spotting;
using Content.Shared.Hands;
using Content.Shared.Interaction.Events;
using Robust.Shared.Map;
using Robust.Shared.Network;

namespace Content.Shared._RMC14.Targeting;

public sealed class TargetingSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<TargetingComponent, ComponentRemove>(OnActiveTargetingLaserRemoved);
        SubscribeLocalEvent<TargetingComponent, DroppedEvent>(OnActiveTargetingLaserDropped);
        SubscribeLocalEvent<TargetingComponent, RMCDroppedEvent>(OnActiveTargetingLaserDropped);
        SubscribeLocalEvent<TargetingComponent, GotUnequippedHandEvent>(OnActiveTargetingLaserDropped);
        SubscribeLocalEvent<TargetingComponent, HandDeselectedEvent>(OnActiveTargetingLaserDropped);
    }

    /// <summary>
    ///     Remove any lasers if the <see cref="TargetingComponent"/> is being removed from an entity.
    /// </summary>
    private void OnActiveTargetingLaserRemoved<T>(Entity<TargetingComponent> targetingLaser, ref T args)
    {
        if (_net.IsClient)
            return;

        while (targetingLaser.Comp.Targets.Count > 0)
        {
            StopTargeting(targetingLaser, targetingLaser.Comp.Targets[0]);
        }
    }

    /// <summary>
    ///     Remove any lasers originating from the entity.
    /// </summary>
    private void OnActiveTargetingLaserDropped<T>(Entity<TargetingComponent> targetingLaser, ref T args)
    {
        var ev = new TargetingCancelledEvent();
        RaiseLocalEvent(targetingLaser, ref ev);

        while (targetingLaser.Comp.Targets.Count > 0)
        {
            StopTargeting(targetingLaser, targetingLaser.Comp.Targets[0]);
        }
    }

    /// <summary>
    ///     Remove the laser and replace targeting effects originating from the given entity.
    /// </summary>
    private void StopTargeting(Entity<TargetingComponent> targetingLaser, EntityUid target)
    {
        if (!TryComp(target, out TargetedComponent? targeted))
            return;

        if (targeted.TargetedBy.Contains(targetingLaser))
        {
            targeted.TargetedBy.Remove(targetingLaser);
            Dirty(target, targeted);
        }

        targetingLaser.Comp.Targets.Remove(target);
        Dirty(targetingLaser);

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

        // Remove the spotted component if none of the targeting lasers is a spotter
        if (!spotters)
            RemComp<SpottedComponent>(target);

        // Update the target marker
        UpdateTargetMarker(target, highestMark, true);

        if (targeted.TargetedBy.Count != 0)
            return;

        // Remove the target's targeted component if it has nothing targeting it anymore
        RemComp<TargetedComponent>(target);

        if(targetingLaser.Comp.Targets.Count == 0)
            RemComp<TargetingComponent>(targetingLaser);
    }

    /// <summary>
    ///     Apply the <see cref="TargetingComponent"/> to the entity creating the laser.
    ///     Apply the <see cref="TargetedComponent"/> to the entity being targeted.
    /// </summary>
    public void Target(EntityUid equipment, EntityUid user, EntityUid target, float laserDuration, TargetedEffects targetedEffect = TargetedEffects.None)
    {
        var targeted = EnsureComp<TargetedComponent>(target);
        targeted.TargetedBy.Add(equipment);
        Dirty(target, targeted);

        var active = EnsureComp<TargetingComponent>(equipment);
        active.Source = equipment;
        active.Targets.Add(target);
        active.Origin = Transform(user).Coordinates;
        active.User = user;
        active.LaserType = targetedEffect;
        active.LaserDurations.Add(laserDuration);
        active.OriginalLaserDurations.Add(laserDuration);
        Dirty(equipment, active);

        UpdateTargetMarker(target, targetedEffect);
    }

    /// <summary>
    ///     Change the enabled visualiser on the target.
    /// </summary>
    private void UpdateTargetMarker(EntityUid target, TargetedEffects newMarker, bool force = false)
    {
        //Get the currently active visualiser.
        _appearance.TryGetData<TargetedEffects>(target, TargetedVisuals.Targeted, out var marker);

        // Only apply the visualiser if forced, or it has a higher priority than the already existing one.
        if (force || newMarker > marker)
            _appearance.SetData(target, TargetedVisuals.Targeted, newMarker);
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<TargetingComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var targeting, out var xform))
        {
            var laserNumber = 0;
            while (laserNumber < targeting.LaserDurations.Count)
            {
                targeting.LaserDurations[laserNumber] -= frameTime;

                // Adjust alpha of the laser based on how close it is to finish targeting.
                targeting.AlphaMultiplier = 1 - targeting.LaserDurations[laserNumber] / targeting.OriginalLaserDurations[laserNumber];
                Dirty(uid,targeting);

                // Raise an event and stop targeting if the targeting successfully finishes.
                if (targeting.LaserDurations[laserNumber] <= 0)
                {
                    var ev = new TargetingFinishedEvent(targeting.User, Transform(targeting.Targets[laserNumber]).Coordinates, targeting.Targets[laserNumber]);
                    RaiseLocalEvent(uid, ref ev);

                    StopTargeting((uid,targeting), targeting.Targets[laserNumber]);
                    targeting.LaserDurations.RemoveAt(laserNumber);
                    targeting.OriginalLaserDurations.RemoveAt(laserNumber);
                    laserNumber = 0;
                }

                laserNumber++;
            }

            // Remove the active component and raise an event if the user moves.
            if (!_transform.InRange(Transform(xform.ParentUid).Coordinates, targeting.Origin, 0.1f))
            {
                var ev = new TargetingCancelledEvent();
                RaiseLocalEvent(uid, ref ev);

                targeting.LaserDurations.Clear();
                targeting.OriginalLaserDurations.Clear();
                RemComp<TargetingComponent>(uid);
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
