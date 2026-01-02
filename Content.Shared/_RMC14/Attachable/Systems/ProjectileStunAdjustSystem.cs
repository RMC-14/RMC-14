using System.Runtime.InteropServices;
using Content.Shared._RMC14.Attachable.Components;
using Content.Shared._RMC14.Attachable.Events;
using Content.Shared._RMC14.Stun;
using Content.Shared.Weapons.Ranged.Events;

namespace Content.Shared._RMC14.Attachable.Systems;

public sealed class ProjectileStunAdjustSystem : EntitySystem
{
    private const string ModifierExamineColour = "yellow";

    public override void Initialize()
    {
        SubscribeLocalEvent<ProjectileStunAdjustComponent, AmmoShotEvent>(OnAmmoShot);
        SubscribeLocalEvent<GrantProjectileStunAdjustComponent, AttachableAlteredEvent>(OnAttachableAltered);
        SubscribeLocalEvent<GrantProjectileStunAdjustComponent, AttachableGetExamineDataEvent>(OnGrantProjectileStunAdjustmentGetExamineData);
    }

    private void OnAmmoShot(Entity<ProjectileStunAdjustComponent> ent, ref AmmoShotEvent args)
    {
        foreach (var projectile in args.FiredProjectiles)
        {
            if (!TryComp(projectile, out RMCStunOnHitComponent? stunComp))
                continue;

            var stuns = CollectionsMarshal.AsSpan(stunComp.Stuns);
            for (var i = 0; i < stuns.Length; i++)
            {
                ref var stun = ref stuns[i];
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

            Dirty(projectile, stunComp);
        }
    }

    private void OnAttachableAltered(Entity<GrantProjectileStunAdjustComponent> ent, ref AttachableAlteredEvent args)
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
                Dirty(args.Holder, stunAdjust);
                break;
            case AttachableAlteredType.Detached:
                RemComp<ProjectileStunAdjustComponent>(args.Holder);
                break;
        }
    }

    private void OnGrantProjectileStunAdjustmentGetExamineData(Entity<GrantProjectileStunAdjustComponent> attachable, ref AttachableGetExamineDataEvent args)
    {
        var effects = new List<string>();
        if (attachable.Comp.StunDurationAdjustment is >= 1.01f or <= 0.99f)
        {
            effects.Add(Loc.GetString("rmc-attachable-examine-ranged-projectile-stun-duration",
                ("colour", ModifierExamineColour),
                ("sign", attachable.Comp.StunDurationAdjustment > 1 ? '+' : ""),
                ("stunDurationMult", attachable.Comp.StunDurationAdjustment - 1)));
        }

        if (!args.Data.ContainsKey(0))
            args.Data[0] = new (null, effects);
        else
            args.Data[0].effectStrings.AddRange(effects);
    }
}
