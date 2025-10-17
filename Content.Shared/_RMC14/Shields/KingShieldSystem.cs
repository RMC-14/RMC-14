using Content.Shared._RMC14.Damage;
using Content.Shared._RMC14.Line;
using Content.Shared._RMC14.Xenonids;
using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
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
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly LineSystem _line = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly MobThresholdSystem _threshold = default!;
    public override void Initialize()
    {
        SubscribeLocalEvent<KingShieldComponent, DamageModifyAfterResistEvent>(OnShieldDamage, before: [typeof(XenoShieldSystem)]);
        SubscribeLocalEvent<KingShieldComponent, RemovedShieldEvent>(OnShieldRemove);

        SubscribeLocalEvent<KingLightningComponent, MoveEvent>(OnLightningMove);
        SubscribeLocalEvent<KingLightningComponent, ComponentShutdown>(OnLightningRemoved);

        SubscribeLocalEvent<XenoBulwarkOfTheHiveComponent, MoveEvent>(OnBulwarkMove);
        SubscribeLocalEvent<XenoBulwarkOfTheHiveComponent, EntityTerminatingEvent>(OnBulwarkDelete);
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

        var time = _timing.CurTime;

        foreach (var ent in _entityLookup.GetEntitiesInRange<XenoComponent>(_transform.GetMapCoordinates(xeno), xeno.Comp.Range))
        {
            if (_mob.IsDead(ent))
                continue;

            if (!_hive.FromSameHive(xeno.Owner, ent.Owner))
                continue;

            EnsureComp<KingShieldComponent>(ent);

            if (_shield.ApplyShield(ent, XenoShieldSystem.ShieldType.King, xeno.Comp.ShieldAmount, duration: xeno.Comp.DecayTime,
                decay: xeno.Comp.DecayAmount, visualState: xeno.Comp.VisualState))
            {
                var lightning = EnsureComp<KingLightningComponent>(ent);
                lightning.Source = xeno;
                lightning.DisappearAt = time + xeno.Comp.LightningDuration;
                xeno.Comp.Supporting.Add(ent);
                UpdateTrail((ent, lightning));
            }
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
            RemCompDeferred<KingShieldComponent>(xeno);
    }

    public void UpdateTrail(Entity<KingLightningComponent> ent)
    {
        if (_net.IsClient)
            return;

        var lightning = ent.Comp;

        if (lightning.StopUpdating)
            return;

        if (lightning.Trail.Count != 0)
            _line.DeleteBeam(lightning.Trail);

        if (_line.TryCreateLine(lightning.Source, ent, lightning.Lightning, out var lines))
            lightning.Trail = lines;
    }

    private void OnBulwarkMove(Entity<XenoBulwarkOfTheHiveComponent> xeno, ref MoveEvent args)
    {
        if (xeno.Comp.Supporting.Count == 0)
            return;

        List<EntityUid> toRemove = new();

        foreach (var supported in xeno.Comp.Supporting)
        {
            if (!TryComp<KingLightningComponent>(supported, out var hookComp))
            {
                toRemove.Add(supported);
                continue;
            }

            UpdateTrail((supported, hookComp));
        }

        foreach (var ent in toRemove)
        {
            xeno.Comp.Supporting.Remove(ent);
        }
    }
    private void OnBulwarkDelete(Entity<XenoBulwarkOfTheHiveComponent> xeno, ref EntityTerminatingEvent args)
    {
        if (xeno.Comp.Supporting.Count == 0)
            return;

        foreach (var support in xeno.Comp.Supporting)
        {
            RemCompDeferred<KingLightningComponent>(support);
        }

        xeno.Comp.Supporting.Clear();
    }

    private void OnLightningMove(Entity<KingLightningComponent> xeno, ref MoveEvent args)
    {
        UpdateTrail(xeno);
    }

    private void OnLightningRemoved(Entity<KingLightningComponent> ent, ref ComponentShutdown args)
    {
        if (TryComp<XenoBulwarkOfTheHiveComponent>(ent.Comp.Source, out var hookSource))
            hookSource.Supporting.Remove(ent);
        ent.Comp.StopUpdating = true;
        Dirty(ent);
        _line.DeleteBeam(ent.Comp.Trail);
    }

    public override void Update(float frameTime)
    {
        if (_net.IsClient)
            return;

        var time = _timing.CurTime;

        var query = EntityQueryEnumerator<KingLightningComponent>();

        while (query.MoveNext(out var uid, out var lightning))
        {
            if (time < lightning.DisappearAt)
                continue;

            RemCompDeferred<KingLightningComponent>(uid);
        }
    }
}
