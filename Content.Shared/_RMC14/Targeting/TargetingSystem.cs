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
        SubscribeLocalEvent<TargetingComponent, ComponentRemove>(OnTargetingRemoved);
        SubscribeLocalEvent<TargetingComponent, DroppedEvent>(OnTargetingDropped);
        SubscribeLocalEvent<TargetingComponent, RMCDroppedEvent>(OnTargetingDropped);
        SubscribeLocalEvent<TargetingComponent, GotUnequippedHandEvent>(OnTargetingDropped);
        SubscribeLocalEvent<TargetingComponent, HandDeselectedEvent>(OnTargetingDropped);
    }

    /// <summary>
    ///     Remove any lasers if the <see cref="TargetingComponent"/> is being removed from an entity.
    /// </summary>
    private void OnTargetingRemoved<T>(Entity<TargetingComponent> targetingLaser, ref T args)
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
    private void OnTargetingDropped<T>(Entity<TargetingComponent> targetingLaser, ref T args)
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
    public void StopTargeting(Entity<TargetingComponent> ent, EntityUid target)
    {
        var targeting = ent.Comp;
        var targetingUid = ent.Owner;

        if (!TryComp(target, out TargetedComponent? targeted))
            return;

        if (targeted.TargetedBy.Contains(targetingUid))
        {
            targeted.TargetedBy.Remove(targetingUid);
            Dirty(target, targeted);
        }

        targeting.Targets.Remove(target);
        targeting.LaserDurations.Remove(target);
        targeting.OriginalLaserDurations.Remove(target);
        Dirty(targetingUid, targeting);

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

        if(targeting.Targets.Count == 0)
            RemComp<TargetingComponent>(targetingUid);
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

        var targeting = EnsureComp<TargetingComponent>(equipment);
        targeting.Source = equipment;
        targeting.LaserDurations.TryAdd(target, laserDuration);
        targeting.OriginalLaserDurations.TryAdd(target, laserDuration);
        targeting.Targets.Add(target);
        targeting.Origin = Transform(user).Coordinates;
        targeting.User = user;
        targeting.LaserType = targetedEffect;
        Dirty(equipment, targeting);

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
            var targetNumber = 0;
            while (targetNumber < targeting.Targets.Count)
            {
                var target = targeting.Targets[targetNumber];
                if (!targeting.LaserDurations.Keys.Contains(target))
                {
                    targetNumber++;
                    continue;
                }
                targeting.LaserDurations[target] -= frameTime;

                // Adjust alpha of the laser based on how close it is to finish targeting.
                targeting.AlphaMultiplier = 1 - targeting.LaserDurations[target] / targeting.OriginalLaserDurations[target];
                Dirty(uid,targeting);

                // Raise an event and stop targeting if the targeting successfully finishes.
                if (targeting.LaserDurations[target] <= 0)
                {
                    targeting.LaserDurations.Remove(target);
                    targeting.OriginalLaserDurations.Remove(target);
                    Dirty(uid,targeting);

                    var ev = new TargetingFinishedEvent(targeting.User, Transform(target).Coordinates, target);
                    RaiseLocalEvent(uid, ref ev);
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
