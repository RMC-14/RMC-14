using Content.Shared._RMC14.Projectiles.Aimed;
using Content.Shared._RMC14.Rangefinder.Spotting;
using Content.Shared._RMC14.Targeting;
using Content.Shared._RMC14.Weapons.Ranged.Homing;
using Content.Shared._RMC14.Weapons.Ranged.Laser;
using Content.Shared._RMC14.Weapons.Ranged.Whitelist;
using Content.Shared.Actions;
using Content.Shared.Popups;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Content.Shared.Whitelist;
using Content.Shared.Wieldable.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;

namespace Content.Shared._RMC14.Weapons.Ranged.AimedShot;

public sealed class AimedShotSystem : EntitySystem
{
    [Dependency] private readonly TargetingSystem _targeting = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedGunSystem _gunSystem = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;

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
    ///     Perform an aimed shot on the entity targeted by the action.
    /// </summary>
    private void OnAimedShot(Entity<AimedShotComponent> ent, ref AimedShotActionEvent args)
    {
        var target = args.Target;
        var user = args.Performer;

        if(!CanAimShot(ent, target, user))
            return;

        args.Handled = true;

        // Add time to the duration of the aimed shot per tile of distance to the target.
        var laserDuration =  (float)(ent.Comp.AimDuration + (_transform.GetMoverCoordinates(target).Position - _transform.GetMoverCoordinates(user).Position).Length() * ent.Comp.AimDistanceDifficulty);
        var appliedSpotterBuff = false;
        var aimMultiplier = 1f;

        // Apply the spotted multiplier if the target is spotted.
        if (TryComp(target, out SpottedComponent? spotted))
        {
            aimMultiplier = spotted.AimDurationMultiplier;
            appliedSpotterBuff = true;
        }

        // Apply the laser's multiplier to the duration of the aimed shot if it's turned on.
        if (TryComp(ent, out GunToggleableLaserComponent? toggleLaser))
        {
            if (toggleLaser.Active)
            {
                if(appliedSpotterBuff)
                    aimMultiplier -= toggleLaser.SpottedAimDurationMultiplierSubtraction;
                else
                    aimMultiplier = toggleLaser.AimDurationMultiplier;
            }

            if (TryComp(ent, out TargetingLaserComponent? targetingLaser))
            {
                targetingLaser.ShowLaser = toggleLaser.Active;
                Dirty(ent, targetingLaser);
            }
        }

        laserDuration *= aimMultiplier;
        ent.Comp.Targets.Add(target);
        Dirty(ent);

        _audio.PlayPredicted(ent.Comp.AimingSound, ent, user);
        _targeting.Target(ent.Owner, user, target, laserDuration, TargetedEffects.Targeted);
    }

    /// <summary>
    ///     Mark projectiles as being shot using the aimed shot action and make them homing.
    ///     <see cref="AimedProjectileComponent"/> and <see cref="HomingProjectileComponent"/>
    /// </summary>
    private void OnAmmoShot(Entity<AimedShotComponent> ent, ref AmmoShotEvent args)
    {
        // This means it's not an aimed shot so don't apply any effects
        if(ent.Comp.CurrentTarget == null)
            return;

        var target = ent.Comp.CurrentTarget.Value;

        // Apply the components that alter the projectiles behavior.
        foreach (var projectile in args.FiredProjectiles)
        {
            var aimedProjectile = EnsureComp<AimedProjectileComponent>(projectile);
            aimedProjectile.Target = target;
            aimedProjectile.Source = ent;
            Dirty(projectile, aimedProjectile);

            var homingProjectile = EnsureComp<HomingProjectileComponent>(projectile);
            homingProjectile.Target = target;
            homingProjectile.ProjectileSpeed = ent.Comp.ProjectileSpeed;
            Dirty(projectile, homingProjectile);
        }

        _targeting.StopTargeting(ent, target);
        RemoveTarget(ent, target);
    }

    /// <summary>
    ///     Try to shoot at a target when a gun successfully finishes targeting.
    /// </summary>
    private void OnTargetingFinished(Entity<AimedShotComponent> ent, ref TargetingFinishedEvent args)
    {
        if(!TryComp(ent, out GunComponent? gun))
            return;

        ent.Comp.CurrentTarget = args.Target;
        Dirty(ent);

        // Only shoot serverside
        if (_net.IsServer)
        {
            var shotProjectiles = _gunSystem.AttemptShoot((ent, gun), args.User, args.Coordinates);

            if (shotProjectiles != null)
            {
                _audio.PlayEntity(gun.SoundGunshotModified, args.User, ent);
            }
            else
            {
                _targeting.StopTargeting(ent, args.Target);
                RemoveTarget(ent, args.Target);

                var ammoCount = new GetAmmoCountEvent();
                RaiseLocalEvent(ent, ref ammoCount);

                if(ammoCount.Count <= 0)
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

        // Can't aim if the user doesn't have the correct whitelist.
        if (!_whitelist.IsValid(ent.Comp.Whitelist, user) && ent.Comp.Whitelist.Components != null)
        {
            var message = Loc.GetString("cm-gun-unskilled", ("gun", ent));
            _popup.PopupClient(message, user, user);

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

        return true;
    }

    /// <summary>
    ///     Remove a target from the target list stored on the component.
    /// </summary>
    /// <param name="ent">The entity that needs the target removed</param>
    /// <param name="target">The target to be removed from the target list</param>
    private void RemoveTarget(Entity<AimedShotComponent> ent, EntityUid target)
    {
        ent.Comp.Targets.Remove(target);
        ent.Comp.CurrentTarget = null;
        Dirty(ent);
    }
}

