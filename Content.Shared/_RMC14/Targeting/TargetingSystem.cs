using Content.Shared._RMC14.Inventory;
using Content.Shared._RMC14.Line;
using Content.Shared._RMC14.Rangefinder.Spotting;
using Content.Shared.Hands;
using Content.Shared.Interaction.Events;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Targeting;

public sealed class TargetingSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly LineSystem _line = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<TargetingComponent, ComponentRemove>(OnActiveTargetingLaserRemoved);
        SubscribeLocalEvent<TargetingComponent, DroppedEvent>(OnActiveTargetingLaserDropped);
        SubscribeLocalEvent<TargetingComponent, RMCDroppedEvent>(OnActiveTargetingLaserDropped);
        SubscribeLocalEvent<TargetingComponent, GotUnequippedHandEvent>(OnActiveTargetingLaserDropped);
        SubscribeLocalEvent<TargetingComponent, HandDeselectedEvent>(OnActiveTargetingLaserDropped);

        //SubscribeLocalEvent<TargetedComponent, MoveEvent>(OnTargetedMove);
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
    ///     Update a laser when it's targeted entity moves //TODO Decide between using this or update.
    /// </summary>
    private void OnTargetedMove(Entity<TargetedComponent> ent, ref MoveEvent args)
    {
        foreach (var targeter in ent.Comp.TargetedBy)
        {
            if(!TryComp(targeter, out TargetingComponent? activelaser))
                return;

            UpdateLaser((targeter,activelaser), ent);
        }
    }

    /// <summary>
    ///     Remove any lasers originating from the entity.
    /// </summary>
    private void OnActiveTargetingLaserDropped<T>(Entity<TargetingComponent> targetingLaser, ref T args)
    {
        var ev = new AimingCancelledEvent();
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

            if(targeted.Laser.TryGetValue(targetingLaser, out var value))
                _line.DeleteBeam(value);

            Dirty(target, targeted);
        }

        targetingLaser.Comp.Targets.Remove(target);
        Dirty(targetingLaser);

        // Find the next target marker with the highest priority
        var highestMark = TargetedEffects.None;
        var spotters = false;

        // Check all active lasers currently aiming at the target
        foreach (var activeLaser in targeted.TargetedBy)
        {
            if (!TryComp(activeLaser, out TargetingComponent? comp))
                continue;

            if (comp.LaserType > highestMark)
                highestMark = comp.LaserType;

            if (comp.LaserType == TargetedEffects.Spotted)
                spotters = true;
        }

        // Remove the targeted component if none of the targeting lasers is a spotter
        if (!spotters)
            RemComp<SpottedComponent>(target);

        // Update the target marker and remove the active laser component
        UpdateTargetMarker(target, highestMark, true);

        if (targeted.TargetedBy.Count != 0)
            return;

        // Remove the target's targeted component if it has nothing targeting it anymore
        RemComp<TargetedComponent>(target);

        if(targetingLaser.Comp.Targets.Count == 0)
            RemComp<TargetingComponent>(targetingLaser);
    }

    /// <summary>
    ///     Remove any old beams and replace it with a new one.
    /// </summary>
    private void UpdateLaser(Entity<TargetingComponent> ent, Entity<TargetedComponent> target)
    {


        return;
        if (_net.IsClient)
            return;

        if (target.Comp.Laser.TryGetValue(ent, out var beam))
            _line.DeleteBeam(beam);

        if (_line.TryCreateLine(ent.Comp.User, target, ent.Comp.LaserProto, out var lines))
            target.Comp.Laser[ent] = lines;

        Dirty(target);
    }

    /// <summary>
    ///     Apply the <see cref="TargetingComponent"/> to the entity creating the laser.
    ///     Apply the <see cref="TargetedComponent"/> to the entity being targeted.
    ///     Create a line in between the user and the target, then enable the given visualiser on the targeted entity.
    /// </summary>
    public bool TryLaserTarget(EntityUid equipment, EntityUid user, EntityUid target, float laserDuration, EntProtoId laserProto, bool showLaser = true, TargetedEffects targetedEffect = TargetedEffects.None)
    {
        var targeted = EnsureComp<TargetedComponent>(target);
        targeted.TargetedBy.Add(equipment);

        var active = EnsureComp<TargetingComponent>(equipment);
        active.Source = equipment;
        active.Targets.Add(target);
        active.LaserProto = laserProto;
        active.Origin = Transform(user).Coordinates;
        active.User = user;
        active.LaserType = targetedEffect;
        active.LaserDurations.Add(laserDuration);
        active.OriginalLaserDurations.Add(laserDuration);

        Dirty(equipment, active);
        Dirty(target, targeted);

        UpdateTargetMarker(target, targetedEffect);
        if(showLaser)
            UpdateLaser((equipment, active), (target,targeted));

        return true;
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
        while (query.MoveNext(out var uid, out var laser, out var xform))
        {
            var laserNumber = 0;
            while (laserNumber < laser.LaserDurations.Count)
            {
                laser.LaserDurations[laserNumber] -= frameTime;

                // Adjust alpha of the laser based on how close it is to finishing.
                if (laser.GradualAlpha)
                {
                    laser.AlphaMultiplier = 1 - laser.LaserDurations[laserNumber] / laser.OriginalLaserDurations[laserNumber];
                }

                Dirty(uid,laser);
                // Raise an event and remove the active component if the aiming successfully finishes.
                if (laser.LaserDurations[laserNumber] <= 0)
                {
                    var ev = new AimingFinishedEvent(laser.User, Transform(laser.Targets[laserNumber]).Coordinates, laser.Targets[laserNumber]);
                    RaiseLocalEvent(uid, ref ev);


                    StopTargeting((uid,laser), laser.Targets[laserNumber]);
                    laser.LaserDurations.RemoveAt(laserNumber);
                    laser.OriginalLaserDurations.RemoveAt(laserNumber);
                    laserNumber = 0;
                }

                laserNumber++;
            }

            // Remove the active component and raise an event if the user moves.
            if (!_transform.InRange(Transform(xform.ParentUid).Coordinates, laser.Origin, 0.1f))
            {
                var ev = new AimingCancelledEvent();
                RaiseLocalEvent(uid, ref ev);

                laser.LaserDurations.Clear();
                laser.OriginalLaserDurations.Clear();
                RemComp<TargetingComponent>(uid);
            }
        }
    }
}

/// <summary>
///     Raised on an entity when it finishes aiming.
/// </summary>
[ByRefEvent]
public record struct AimingFinishedEvent(EntityUid User, EntityCoordinates Coordinates, EntityUid Target, bool Handled = false);

/// <summary>
///     Raised on an aiming when it's aiming is interrupted.
/// </summary>
[ByRefEvent]
public record struct AimingCancelledEvent(bool Handled = false);
