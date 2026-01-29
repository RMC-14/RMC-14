using Content.Shared._RMC14.Projectiles.Aimed;
using Content.Shared._RMC14.Rangefinder.Spotting;
using Content.Shared._RMC14.Targeting;
using Content.Shared._RMC14.Weapons.Ranged.Homing;
using Content.Shared._RMC14.Weapons.Ranged.Laser;
using Content.Shared._RMC14.Weapons.Ranged.Whitelist;
using Content.Shared.Actions;
using Content.Shared.CombatMode;
using Content.Shared.Examine;
using Content.Shared.Popups;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Content.Shared.Whitelist;
using Content.Shared.Wieldable.Components;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Weapons.Ranged.AimedShot;

public abstract class SharedRMCAimedShotSystem : EntitySystem
{
    [Dependency] private readonly SharedRMCTargetingSystem _targeting = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedGunSystem _gunSystem = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly ExamineSystemShared _examine = default!;
    [Dependency] private readonly SharedCombatModeSystem _combatMode = default!;

    [Dependency] protected readonly IGameTiming Timing = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<AimedShotComponent, GetItemActionsEvent>(OnAimedShotGetItemActions);
        SubscribeLocalEvent<AimedShotComponent, AimedShotActionEvent>(OnAimedShot);
        SubscribeLocalEvent<AimedShotComponent, TargetingFinishedEvent>(OnTargetingFinished);
        SubscribeLocalEvent<AimedShotComponent, TargetingCancelledEvent>(OnTargetingCancelled);
        SubscribeLocalEvent<AimedShotComponent, AmmoShotEvent>(OnAmmoShot);
    }

    /// <summary>
    ///     Give the action to the entity holding the item.
    /// </summary>
    private void OnAimedShotGetItemActions(Entity<AimedShotComponent> ent, ref GetItemActionsEvent args)
    {
        args.AddAction(ref ent.Comp.Action, ent.Comp.ActionId);

        // Inherit the whitelist from the equipment giving the action.
        if (TryComp(args.Provider, out GunUserWhitelistComponent? whitelist))
            ent.Comp.Whitelist = whitelist.Whitelist;
        else
            ent.Comp.Whitelist = new EntityWhitelist();

        Dirty(ent);
    }

    /// <summary>
    ///     Try to perform an aimed shot on the given target.
    /// </summary>
    /// <param name="netGun">The gun performing the aimed shot.</param>
    /// <param name="netUser">The entity using the gun to perform the aimed shot.</param>
    /// <param name="netTarget">The target of the aimed shot.</param>
    protected void AimedShotRequested(NetEntity netGun, NetEntity netUser, NetEntity netTarget)
    {
        var gun = GetEntity(netGun);
        var user = GetEntity(netUser);
        var target = GetEntity(netTarget);

        if (!TryComp(gun, out AimedShotComponent? aimedShot) || !aimedShot.Activated)
            return;

        if(!CanAimShot((gun, aimedShot), target, user))
            return;

        // Add time to the duration of the aimed shot per tile of distance to the target.
        var laserDuration =  (float)(aimedShot.AimDuration + (_transform.GetMoverCoordinates(target).Position - _transform.GetMoverCoordinates(user).Position).Length() * aimedShot.AimDistanceDifficulty);
        var appliedSpotterBuff = false;
        var aimMultiplier = 1f;
        var targetEffect = aimedShot.TargetEffect;
        var showDirection = aimedShot.ShowDirection;

        // Apply the spotted multiplier if the target is spotted.
        if (TryComp(target, out SpottedComponent? spotted))
        {
            aimMultiplier = spotted.AimDurationMultiplier;
            appliedSpotterBuff = true;
        }

        // Apply the laser's multiplier to the duration of the aimed shot if it's turned on.
        if (TryComp(gun, out GunToggleableLaserComponent? toggleLaser))
        {
            if (toggleLaser.Active)
            {
                if(appliedSpotterBuff)
                    aimMultiplier -= toggleLaser.SpottedAimDurationMultiplierSubtraction;
                else
                    aimMultiplier = toggleLaser.AimDurationMultiplier;

                showDirection = false;
            }

            if (TryComp(gun, out TargetingLaserComponent? targetingLaser))
            {
                targetingLaser.ShowLaser = toggleLaser.Active;
                Dirty(gun, targetingLaser);
            }
        }

        laserDuration *= aimMultiplier;
        aimedShot.Targets.Add(target);
        aimedShot.NextAimedShot = Timing.CurTime + aimedShot.AimedShotCooldown;
        Dirty(gun, aimedShot);

        _audio.PlayPredicted(aimedShot.AimingSound, gun, user);
        _targeting.Target(gun, user, target, laserDuration, targetEffect, showDirection);
    }

    /// <summary>
    ///     Toggle the ability to perform an aimed shot using the unique action key on or off.
    /// </summary>
    private void OnAimedShot(Entity<AimedShotComponent> ent, ref AimedShotActionEvent args)
    {
        ent.Comp.Activated = !ent.Comp.Activated;
        Dirty(ent);

        _actions.SetToggled(ent.Comp.Action, ent.Comp.Activated);
        args.Handled = true;
    }

    /// <summary>
    ///     Mark projectiles as being shot using the aimed shot action and make them homing.
    ///     <see cref="AimedProjectileComponent"/> and <see cref="HomingProjectileComponent"/>
    /// </summary>
    private void OnAmmoShot(Entity<AimedShotComponent> ent, ref AmmoShotEvent args)
    {
        // This means it's not an aimed shot so don't apply any effects
        if(ent.Comp.CurrentTarget == null || TerminatingOrDeleted(ent.Comp.CurrentTarget))
            return;

        var target = ent.Comp.CurrentTarget.Value;

        // Apply the components that alter the projectiles behavior.
        foreach (var projectile in args.FiredProjectiles)
        {
            var ev = new AimedShotEvent(target);
            RaiseLocalEvent(ent, ref ev);

            var aimedProjectile = EnsureComp<AimedProjectileComponent>(projectile);
            aimedProjectile.Target = target;
            aimedProjectile.Source = ent;
            Dirty(projectile, aimedProjectile);

            var homingProjectile = EnsureComp<HomingProjectileComponent>(projectile);
            homingProjectile.Target = target;
            homingProjectile.ProjectileSpeed = ent.Comp.ProjectileSpeed;
            Dirty(projectile, homingProjectile);

            // Make sure the projectile can hit dead targets.
            var targeted = EnsureComp<TargetedProjectileComponent>(projectile);
            targeted.Target = target;
            Dirty(projectile, targeted);

            var ev2 = new ShotByAimedShotEvent(ent, target);
            RaiseLocalEvent(projectile, ref ev2);
        }

        RemoveTarget(ent, target);
    }

    /// <summary>
    ///     Try to shoot at a target when a gun successfully finishes targeting.
    /// </summary>
    private void OnTargetingFinished(Entity<AimedShotComponent> ent, ref TargetingFinishedEvent args)
    {
        if(!TryComp(ent, out GunComponent? gun))
            return;

        // Don't shoot if the target isn't visible when aiming is finished.
        if (!_examine.InRangeUnOccluded(args.User, args.Target, ent.Comp.Range))
        {
            RemoveTarget(ent, args.Target);
            var message = Loc.GetString("rmc-action-popup-aiming-target-blocked", ("gun", ent));
            _popup.PopupClient(message, args.User, args.User);
            return;
        }

        ent.Comp.CurrentTarget = args.Target;
        Dirty(ent);

        // Only shoot serverside
        if (_net.IsServer)
        {
            var shotProjectiles = _gunSystem.AttemptShoot((ent, gun), args.User, args.Coordinates);

            var ammoCount = new GetAmmoCountEvent();
            RaiseLocalEvent(ent, ref ammoCount);

            if (shotProjectiles != null)
            {
                _audio.PlayEntity(gun.SoundGunshotModified, args.User, ent);
                if (ammoCount.Count == 0)
                    _audio.PlayPvs(new SoundPathSpecifier("/Audio/Weapons/Guns/EmptyAlarm/smg_empty_alarm.ogg"), ent);
            }
            else
            {
                RemoveTarget(ent, args.Target);

                if(ammoCount.Count < 0)
                    _audio.PlayEntity(gun.SoundEmpty, args.User, ent);
            }
        }
        // Update ammo visualiser because client doesn't know about the shot.
        var ev = new UpdateClientAmmoEvent();
        RaiseLocalEvent(ent, ref ev);
    }

    /// <summary>
    ///     Clear targets when targeting is cancelled.
    /// </summary>
    private void OnTargetingCancelled(Entity<AimedShotComponent> ent, ref TargetingCancelledEvent args)
    {
        if(args.Handled)
            return;

        ent.Comp.Targets.Clear();
        Dirty(ent);

        args.Handled = true;
    }

    /// <summary>
    ///     Checks if it's possible to use the aimed shot action on the selected target.
    /// </summary>
    private bool CanAimShot(Entity<AimedShotComponent> ent, EntityUid target, EntityUid user)
    {
        // Can't aim at the target if they don't have the component that marks them as a potential target.
        if (!HasComp<SpottableComponent>(target))
            return false;

        // Can't aim at a target you can't see.
        if (!_examine.InRangeUnOccluded(user, target, ent.Comp.Range))
            return false;

        // Can't aim for a certain amount of time after having performed an aimed shot.
        if (ent.Comp.NextAimedShot > Timing.CurTime)
            return false;

        // Can't aim if not in combat mode.
        if (!_combatMode.IsInCombatMode(user))
            return false;

        // Can't aim if the user doesn't have the correct whitelist.
        if (!_whitelist.IsValid(ent.Comp.Whitelist, user) && ent.Comp.Whitelist.Components != null)
        {
            var message = Loc.GetString("cm-gun-unskilled", ("gun", ent));
            _popup.PopupClient(message, user, user, PopupType.SmallCaution);

            return false;
        }

        // Can't aim if the user isn't wielding the gun.
        if (TryComp(ent, out WieldableComponent? wieldable) &&
            !wieldable.Wielded)
        {
            var message = Loc.GetString("rmc-action-popup-aiming-user-must-wield", ("gun", ent));
            _popup.PopupClient(message, user, user);

            return false;
        }

        // Can't aim if the gun is empty.
        var ammoCount = new GetAmmoCountEvent();
        RaiseLocalEvent(ent, ref ammoCount);
        if (ammoCount.Count <= 0)
        {
            var message = Loc.GetString("rmc-action-popup-aiming-gun-no-ammo", ("gun", ent));
            _popup.PopupClient(message, user, user);

            return false;
        }

        // Can't aim if the target is too close.
        if (_transform.InRange(Transform(target).Coordinates, Transform(user).Coordinates, ent.Comp.MinRange))
        {
            var message = Loc.GetString("rmc-action-popup-aiming-target-too-close", ("target", target));
            _popup.PopupClient(message, user, user);

            return false;
        }

        return true;
    }

    /// <summary>
    ///     Remove a target from the target list stored on the component.
    /// </summary>
    /// <param name="ent">The entity that needs the target removed</param>
    /// <param name="target">The target to be removed from the target list</param>
    private void RemoveTarget(Entity<AimedShotComponent> ent, EntityUid target)
    {
        _targeting.StopTargeting(ent.Owner, target);
        ent.Comp.Targets.Remove(target);
        ent.Comp.CurrentTarget = null;
        Dirty(ent);
    }
}

/// <summary>
///     Raised on a gun when it shoots using the aimed shot action.
/// </summary>
/// <param name="Target">The target of the aimed shot.</param>
[ByRefEvent]
public record struct AimedShotEvent(EntityUid Target);

/// <summary>
///     Raised on a projectile when it was shot using the aimed shot action.
/// </summary>
/// <param name="Target">The target of the aimed shot.</param>
[ByRefEvent]
public record struct ShotByAimedShotEvent(EntityUid Gun, EntityUid Target);

