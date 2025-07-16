using Content.Shared._RMC14.Attachable.Components;
using Content.Shared._RMC14.Attachable.Events;
using Content.Shared._RMC14.Stun;
using Content.Shared.Weapons.Ranged.Events;

namespace Content.Shared._RMC14.Attachable.Systems;

public sealed class ProjectileStunAdjustSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<ProjectileStunAdjustComponent, AmmoShotEvent>(ProjectileStunRemove);
        SubscribeLocalEvent<GrantProjectileStunAdjustComponent, AttachableAlteredEvent>(CheckProjectileStunRemove);
    }

    private void ProjectileStunRemove(Entity<ProjectileStunAdjustComponent> ent, ref AmmoShotEvent args)
    {
        foreach (var projectile in args.FiredProjectiles)
        {
            if (TryComp(projectile, out RMCStunOnHitComponent? stun))
            {
                stun.StunTime *= ent.Comp.StunDurationAdjustment;
                stun.DazeTime *= ent.Comp.DazeDurationAdjustment;
                stun.MaxRange *= ent.Comp.MaxRangeAdjustment;
                stun.ForceKnockBack = ent.Comp.ForceKnockBackAdjustment;
                stun.KnockBackPowerMin *= ent.Comp.KnockBackPowerMinAdjustment;
                stun.KnockBackPowerMax *= ent.Comp.KnockBackPowerMaxAdjustment;
                stun.LosesEffectWithRange = ent.Comp.LosesEffectWithRangeAdjustment;
                stun.SlowsEffectBigXenos = ent.Comp.SlowsEffectBigXenosAdjustment;
                stun.SuperSlowTime *= ent.Comp.SuperSlowTimeAdjustment;
                stun.SlowTime *= ent.Comp.SlowTimeAdjustment;
                stun.StunArea += ent.Comp.StunAreaAdjustment;
            }
        }
    }

    private void CheckProjectileStunRemove(Entity<GrantProjectileStunAdjustComponent> ent, ref AttachableAlteredEvent args)
    {
        switch (args.Alteration)
        {
            case AttachableAlteredType.Attached:
                var stunAdjust = EnsureComp<ProjectileStunAdjustComponent>(args.Holder);
                stunAdjust.StunDurationAdjustment = ent.Comp.StunDurationAdjustment;
                stunAdjust.DazeDurationAdjustment = ent.Comp.DazeDurationAdjustment;
                stunAdjust.MaxRangeAdjustment = ent.Comp.MaxRangeAdjustment;
                stunAdjust.ForceKnockBackAdjustment = ent.Comp.ForceKnockBackAdjustment;
                stunAdjust.KnockBackPowerMinAdjustment = ent.Comp.KnockBackPowerMinAdjustment;
                stunAdjust.KnockBackPowerMaxAdjustment = ent.Comp.KnockBackPowerMaxAdjustment;
                stunAdjust.LosesEffectWithRangeAdjustment = ent.Comp.LosesEffectWithRangeAdjustment;
                stunAdjust.SlowsEffectBigXenosAdjustment  = ent.Comp.SlowsEffectBigXenosAdjustment;
                stunAdjust.SuperSlowTimeAdjustment = ent.Comp.SuperSlowTimeAdjustment;
                stunAdjust.SlowTimeAdjustment = ent.Comp.SlowTimeAdjustment;
                stunAdjust.StunAreaAdjustment = ent.Comp.StunAreaAdjustment;
                break;
            case AttachableAlteredType.Detached:
                RemComp<ProjectileStunAdjustComponent>(args.Holder);
                break;
        }
    }
}
