using System.Numerics;
using Content.Shared._RMC14.Projectiles.Aimed;
using Content.Shared._RMC14.Stun;
using Content.Shared._RMC14.Weapons.Ranged.AimedShot;
using Content.Shared._RMC14.Weapons.Ranged.AimedShot.FocusedShooting;
using Content.Shared._RMC14.Xenonids.Fortify;
using Content.Shared.Camera;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Projectiles;
using Content.Shared.Stunnable;

namespace Content.Shared._RMC14.Projectiles.StoppingPower;

public sealed class RMCStoppingPowerSystem : EntitySystem
{
    [Dependency] private readonly RMCSizeStunSystem _sizeStun = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly StaminaSystem _stamina = default!;
    [Dependency] private readonly SharedCameraRecoilSystem _cameraRecoil = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCStoppingPowerComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<RMCStoppingPowerComponent, ProjectileHitEvent>(OnStoppingPowerHit);
        SubscribeLocalEvent<RMCStoppingPowerComponent, ShotByAimedShotEvent>(OnShotByAimedShot);
    }

    private void OnMapInit(Entity<RMCStoppingPowerComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.ShotFrom = _transform.GetMoverCoordinates(ent);
        Dirty(ent);
    }

    private void OnShotByAimedShot(Entity<RMCStoppingPowerComponent> ent, ref ShotByAimedShotEvent args)
    {
        if (!TryComp<RMCFocusedShootingComponent>(args.Gun, out var focused))
            return;

        ent.Comp.FocusedCounter = focused.FocusCounter;
    }

    /// <summary>
    ///     Apply a stun and/or knockback based on the damage of the shot and what kind of target is hit.
    /// </summary>
    private void OnStoppingPowerHit(Entity<RMCStoppingPowerComponent> ent, ref ProjectileHitEvent args)
    {
        ent.Comp.CurrentStoppingPower = 0;
        Dirty(ent);

        if (ent.Comp.RequiresAimedShot && !TryComp<AimedProjectileComponent>(ent, out var aimedShot))
            return;

        if (ent.Comp.FocusedCounterThreshold != null && ent.Comp.FocusedCounter < ent.Comp.FocusedCounterThreshold)
            return;

        var stoppingPower = (float)Math.Min(Math.Ceiling(args.Damage.GetTotal().Float() / ent.Comp.StoppingPowerDivider), ent.Comp.MaxStoppingPower);

        if (!(stoppingPower > ent.Comp.StoppingThreshold))
            return;

        var target = args.Target;
        _sizeStun.TryGetSize(target, out var size);

        ent.Comp.CurrentStoppingPower = stoppingPower;
        Dirty(ent);

        // Big xenos have higher thresholds before they get affected.
        if (size >= RMCSizes.Big)
        {
            if (stoppingPower >= ent.Comp.BigXenoScreenShakeThreshold)
            {
                _cameraRecoil.KickCamera(target, new Vector2(stoppingPower - 3, stoppingPower - 2));
            }

            if (stoppingPower >= ent.Comp.BigXenoInterruptThreshold)
            {
                // Queen and crusher need to be the target of an aimed shot to be stunned.
                if (size >= RMCSizes.Immobile)
                {
                    if(!TryComp(ent, out AimedProjectileComponent? aimedProjectile))
                        return;

                    if(aimedProjectile.Target != target)
                        return;
                }
                //Rotate a fortified defender since they can't be knocked down.
                if (HasComp<XenoFortifyComponent>(target))
                {
                    _transform.SetWorldRotation(target, _transform.GetWorldRotation(target) + Angle.FromDegrees(45));
                }

                _stun.TryParalyze(target, ent.Comp.BigXenoStunTime, true);
                SendMessage(target, Loc.GetString("rmc-xeno-stun-interrupt-shaken"), PopupType.SmallCaution);
            }
            else
            {
                SendMessage(target, Loc.GetString("rmc-xeno-shaken"));
            }
        }
        // Anything that's not a big xeno gets the full effects.
        else
        {
            _cameraRecoil.KickCamera(target, new Vector2(stoppingPower - 2, stoppingPower - 1));

            // Don't knock back if knocked down.
            if(!HasComp<KnockedDownComponent>(target) && !_mobState.IsDead(target))
            {
                _sizeStun.KnockBack(target, ent.Comp.ShotFrom);

                SendMessage(target, Loc.GetString("rmc-xeno-knocked-back"), PopupType.SmallCaution);
            }
            else
            {
                SendMessage(target, Loc.GetString("rmc-xeno-shaken"));
            }

            var stunTime = TimeSpan.FromSeconds((stoppingPower - ent.Comp.StoppingThreshold) * ent.Comp.XenoStunMultiplier);

            if (size >= RMCSizes.VerySmallXeno)
            {
                _stun.TryParalyze(target, stunTime, true);
            }
            else
            {
                // Apply the full projectile damage as stamina damage if possible.
                if (HasComp<StaminaComponent>(target))
                {
                    _stamina.TakeStaminaDamage(target, args.Damage.GetTotal().Float());
                }
                // Knock the target down if the target has no stamina component.
                else
                {
                    _stun.TryParalyze(target, stunTime, true);
                }
            }
        }
    }

    private void SendMessage(EntityUid target, string message, PopupType popupType = PopupType.Small)
    {
        _popup.PopupEntity(message, target, target, popupType);
    }
}
