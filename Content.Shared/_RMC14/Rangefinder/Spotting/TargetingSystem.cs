using Content.Shared._RMC14.Inventory;
using Content.Shared._RMC14.Line;
using Content.Shared.Hands;
using Content.Shared.Interaction.Events;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Rangefinder.Spotting;

public sealed class TargetingSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly LineSystem _line = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ActiveTargetingLaserComponent, ComponentRemove>(OnActiveTargetingLaserRemoved);
        SubscribeLocalEvent<ActiveTargetingLaserComponent, DroppedEvent>(OnActiveTargetingLaserDropped);
        SubscribeLocalEvent<ActiveTargetingLaserComponent, RMCDroppedEvent>(OnActiveTargetingLaserDropped);
        SubscribeLocalEvent<ActiveTargetingLaserComponent, GotUnequippedHandEvent>(OnActiveTargetingLaserDropped);
        SubscribeLocalEvent<ActiveTargetingLaserComponent, HandDeselectedEvent>(OnActiveTargetingLaserDropped);
    }

    /// <summary>
    ///     Remove any lasers if the <see cref="ActiveTargetingLaserComponent"/> is being removed from an entity.
    /// </summary>
    private void OnActiveTargetingLaserRemoved<T>(Entity<ActiveTargetingLaserComponent> targetingLaser, ref T args)
    {
        if (_net.IsClient)
            return;

        StopTargeting(targetingLaser);
    }

    /// <summary>
    ///     Remove any lasers originating from the entity.
    /// </summary>
    private void OnActiveTargetingLaserDropped<T>(Entity<ActiveTargetingLaserComponent> targetingLaser, ref T args)
    {
        var ev = new AimingCancelledEvent();
        RaiseLocalEvent(targetingLaser, ref ev);
        StopTargeting(targetingLaser);
    }

    /// <summary>
    ///     Remove the laser and replace targeting effects originating from the given entity.
    /// </summary>
    private void StopTargeting(Entity<ActiveTargetingLaserComponent> targetingLaser)
    {
        _line.DeleteBeam(targetingLaser.Comp.Laser);

        if (!TryComp(targetingLaser.Comp.Target, out TargetedComponent? targeted))
            return;

        targeted.TargetedBy.Remove(targetingLaser);
        Dirty(targetingLaser.Comp.Target, targeted);

        // Find the next target marker with the highest priority
        var highestMark = TargetedEffects.None;
        var spotters = false;

        // Check all active lasers currently aiming at the target
        foreach (var activeLaser in targeted.TargetedBy)
        {
            if(!TryComp(activeLaser, out ActiveTargetingLaserComponent? comp))
                continue;

            if (comp.LaserType > highestMark)
                highestMark = comp.LaserType;

            if (comp.LaserType == TargetedEffects.Spotted)
                spotters = true;
        }

        // Remove the targeted component if none of the targeting lasers is a spotter
        if(!spotters)
            RemComp<SpottedComponent>(targetingLaser.Comp.Target);

        // Update the target marker and remove the active laser component
        UpdateTargetMarker(targetingLaser.Comp.Target, highestMark, true);
        RemComp<ActiveTargetingLaserComponent>(targetingLaser);

        if(targeted.TargetedBy.Count != 0)
            return;

        // Remove the target's targeted component if it has nothing targeting it anymore
        RemComp<TargetedComponent>(targetingLaser.Comp.Target);
    }

    /// <summary>
    ///     Remove any old beams and replace it with a new one.
    /// </summary>
    private void UpdateLaser(Entity<ActiveTargetingLaserComponent> ent)
    {
        if (_net.IsClient)
            return;

        var laser = ent.Comp;

        if (laser.Laser.Count != 0)
            _line.DeleteBeam(laser.Laser);

        if (_line.TryCreateLine(laser.User, laser.Target, laser.LaserProto, out var lines))
            laser.Laser = lines;
    }

    /// <summary>
    ///     Apply the <see cref="ActiveTargetingLaserComponent"/> to the entity creating the laser.
    ///     Apply the <see cref="TargetedComponent"/> to the entity being targeted.
    ///     Create a line in between the user and the target, then enable the given visualiser on the targeted entity.
    /// </summary>
    public bool TryLaserTarget(EntityUid equipment, EntityUid user, EntityUid target, double laserDuration, EntProtoId laserProto, bool showLaser = true, TargetedEffects targetedEffect = TargetedEffects.None)
    {
        var targeted = EnsureComp<TargetedComponent>(target);
        targeted.TargetedBy.Add(equipment);

        var active = EnsureComp<ActiveTargetingLaserComponent>(equipment);
        active.Source = equipment;
        active.Target = target;
        active.LaserProto = laserProto;
        active.Origin = Transform(user).Coordinates;
        active.User = user;
        active.LaserType = targetedEffect;
        active.LaserDuration = laserDuration;
        active.ShowLaser = showLaser;

        Dirty(equipment, active);
        Dirty(target, targeted);

        UpdateTargetMarker(target, targetedEffect);
        if(showLaser)
            UpdateLaser((equipment, active));

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
        var query = EntityQueryEnumerator<ActiveTargetingLaserComponent, TransformComponent>();

        while (query.MoveNext(out var uid, out var laser, out var xform))
        {
            laser.LaserDuration -= frameTime;
            Dirty(uid,laser);

            // Only update the laser if it's actually supposed to be visible.
            if (laser.ShowLaser)
                UpdateLaser((uid, laser));

            // Raise an event and remove the active component if the aiming successfully finishes.
            if (laser.LaserDuration <= 0)
            {
                var ev = new AimingFinishedEvent(laser.User, Transform(laser.Target).Coordinates);
                RaiseLocalEvent(uid, ref ev);
                RemComp<ActiveTargetingLaserComponent>(uid);
            }

            // Remove the active component and raise an event if the user moves.
            if (!_transform.InRange(Transform(xform.ParentUid).Coordinates, laser.Origin, 0.1f))
            {
                var ev = new AimingCancelledEvent();
                RaiseLocalEvent(uid, ref ev);
                RemComp<ActiveTargetingLaserComponent>(uid);
            }
        }
    }
}

/// <summary>
///     Raised on an entity when it finishes aiming.
/// </summary>
[ByRefEvent]
public record struct AimingFinishedEvent(EntityUid User, EntityCoordinates Coordinates, bool Handled = false);

/// <summary>
///     Raised on an aiming when it's aiming is interrupted.
/// </summary>
[ByRefEvent]
public record struct AimingCancelledEvent(bool Handled = false);
