using Content.Shared._RMC14.Projectiles.Aimed;
using Content.Shared._RMC14.Rangefinder.Spotting;
using Content.Shared._RMC14.Targeting;
using Content.Shared._RMC14.Weapons.Ranged.Homing;
using Content.Shared._RMC14.Weapons.Ranged.Laser;
using Content.Shared._RMC14.Weapons.Ranged.Whitelist;
using Content.Shared.Actions;
using Content.Shared.Popups;
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
        SubscribeLocalEvent<AimedShotComponent, ShotAttemptedEvent>(OnAttemptShoot);
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
        if (args.Handled)
            return;

        // Cancel the action if the user doesn't have the correct whitelist
        if (!_whitelist.IsValid(ent.Comp.Whitelist, args.Performer) && ent.Comp.Whitelist.Components != null)
        {
            var message = Loc.GetString("cm-gun-unskilled", ("gun", ent));
            _popup.PopupClient(message, args.Performer, args.Performer);

            return;
        }

        // cancel the action if the user isn't wielding the gun.
        if (TryComp(ent, out WieldableComponent? wieldable) &&
            !wieldable.Wielded)
        {
            var message = Loc.GetString("rmc-action-popup-aiming-user-must-wield", ("gun", ent));
            _popup.PopupClient(message, args.Performer, args.Performer);

            return;
        }

        // Disable shooting until targeting is cancelled or finished.
        ToggleShooting((ent.Owner, ent.Comp),true);

        // Do play the sound clientside
        _audio.PlayPredicted(ent.Comp.AimingSound, ent, args.Performer);

        args.Handled = true;

        // Add time to the duration of the aimed shot per tile of distance to the target.
        var laserDuration =  (float)(ent.Comp.AimDuration + (_transform.GetMoverCoordinates(args.Target).Position - _transform.GetMoverCoordinates(args.Performer).Position).Length() * ent.Comp.AimDistanceDifficulty);
        var appliedSpotterBuff = false;
        var aimMultiplier = 1f;

        // Apply the spotted multiplier if the target is spotted.
        if (TryComp(args.Target, out SpottedComponent? spotted))
        {
            aimMultiplier = spotted.AimDurationMultiplier;
            appliedSpotterBuff = true;
        }


        // Apply the laser's multiplier to the duration of the aimed shot  if it's turned on.
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

        _targeting.Target(ent.Owner, args.Performer, args.Target, laserDuration, TargetedEffects.Targeted);
    }

    /// <summary>
    ///     Cancel any shots made while aiming.
    /// </summary>
    private void OnAttemptShoot(Entity<AimedShotComponent> ent, ref ShotAttemptedEvent args)
    {
        if(args.Cancelled)
            return;

        if(ent.Comp.WaitForAiming)
            args.Cancel();
    }

    /// <summary>
    ///     Mark projectiles as being shot using the aimed shot action and make them homing.
    /// </summary>
    private void OnAmmoShot(Entity<AimedShotComponent> ent, ref AmmoShotEvent args)
    {
        if(ent.Comp.Target == null)
            return;

        foreach (var projectile in args.FiredProjectiles)
        {
            var aimedProjectile = EnsureComp<AimedProjectileComponent>(projectile);
            aimedProjectile.Target = ent.Comp.Target.Value;
            Dirty(projectile, aimedProjectile);

            var homingProjectile = EnsureComp<HomingProjectileComponent>(projectile);
            homingProjectile.Target = ent.Comp.Target.Value;
            homingProjectile.ProjectileSpeed = ent.Comp.ProjectileSpeed;
            Dirty(projectile, homingProjectile);
        }

        ent.Comp.Target = null;
        Dirty(ent);
    }

    /// <summary>
    ///     Try to shoot at a target when an entity successfully finishes targeting.
    /// </summary>
    private void OnTargetingFinished(Entity<AimedShotComponent> ent, ref TargetingFinishedEvent args)
    {
        if (args.Handled)
            return;

        _gunSystem.TryGetGun(ent, out var gun, out var gunComp);
        if(gunComp == null)
            return;

        args.Handled = true;
        ent.Comp.Target = args.Target;

        // Enable the ability to shoot when done aiming.
        ToggleShooting(ent, false);

        _gunSystem.AttemptShoot(args.User,gun, gunComp, args.Coordinates);
    }

    /// <summary>
    ///     Enable the ability to shoot when targeting is cancelled.
    /// </summary>
    private void OnTargetingCancelled(Entity<AimedShotComponent> ent, ref TargetingCancelledEvent args)
    {
        if(args.Handled)
            return;

        ToggleShooting(ent, false);

        args.Handled = true;
    }

    /// <summary>
    ///     Enable or disable the ability to shoot the gun.
    /// </summary>
    private void ToggleShooting(Entity<AimedShotComponent> ent, bool wait)
    {
        ent.Comp.WaitForAiming = wait;
        Dirty(ent);
    }
}

