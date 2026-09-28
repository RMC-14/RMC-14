using Content.Shared._RMC14.Damage;
using Content.Shared._RMC14.Tether;
using Content.Shared._RMC14.Xenonids;
using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Shields;

public sealed class KingShieldSystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly XenoShieldSystem _shield = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly MobStateSystem _mob = default!;
    [Dependency] private readonly SharedXenoHiveSystem _hive = default!;
    [Dependency] private readonly MobThresholdSystem _threshold = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<KingShieldComponent, DamageModifyAfterResistEvent>(OnShieldDamage, before: [typeof(XenoShieldSystem)]);
        SubscribeLocalEvent<KingShieldComponent, RemovedShieldEvent>(OnShieldRemove);

        SubscribeLocalEvent<XenoBulwarkOfTheHiveComponent, XenoBulwarkOfTheHiveActionEvent>(OnXenoBulwarkOfTheHiveAction);
    }

    private void OnXenoBulwarkOfTheHiveAction(Entity<XenoBulwarkOfTheHiveComponent> xeno, ref XenoBulwarkOfTheHiveActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        _audio.PlayPredicted(xeno.Comp.Sound, xeno, xeno);
        EnsureComp<KingShieldComponent>(xeno);
        _shield.ApplyShield(xeno, XenoShieldSystem.ShieldType.King, xeno.Comp.ShieldAmount, duration: xeno.Comp.DecayTime,
            decay: xeno.Comp.DecayAmount, visualState: xeno.Comp.VisualState);

        foreach (var ent in _entityLookup.GetEntitiesInRange<XenoComponent>(_transform.GetMapCoordinates(xeno), xeno.Comp.Range))
        {
            if (_mob.IsDead(ent))
                continue;

            if (!_hive.FromSameHive(xeno.Owner, ent.Owner))
                continue;

            EnsureComp<KingShieldComponent>(ent);

            if (!_shield.ApplyShield(ent, XenoShieldSystem.ShieldType.King, xeno.Comp.ShieldAmount, duration: xeno.Comp.DecayTime, decay: xeno.Comp.DecayAmount, visualState: xeno.Comp.VisualState))
                continue;

            var tether = EnsureComp<RMCTetherComponent>(ent);
            tether.TetherOrigin = xeno;
            tether.RsiPath = xeno.Comp.LightningRsiPath;
            tether.TetherState = xeno.Comp.LightningEffectState;
            tether.TetherWidth = xeno.Comp.LightningWidth;
            tether.RemoveAt = _timing.CurTime + xeno.Comp.LightningDuration;
            Dirty(ent, tether);
        }
    }

    private void OnShieldDamage(Entity<KingShieldComponent> xeno, ref DamageModifyAfterResistEvent args)
    {
        if (!TryComp<XenoShieldComponent>(xeno, out var shield))
            return;

        if (!shield.Active || shield.Shield != XenoShieldSystem.ShieldType.King)
            return;

        if (!_threshold.TryGetIncapThreshold(xeno, out var threshold))
            _threshold.TryGetDeadThreshold(xeno, out threshold);

        var maxDamage = threshold * xeno.Comp.MaxDamagePercent;

        if (maxDamage != null)
            args.Damage.ClampMax(maxDamage.Value);
    }

    private void OnShieldRemove(Entity<KingShieldComponent> xeno, ref RemovedShieldEvent args)
    {
        if (args.Type == XenoShieldSystem.ShieldType.King)
        {
            RemCompDeferred<KingShieldComponent>(xeno);
            RemComp<RMCTetherComponent>(xeno);
        }
    }
}
