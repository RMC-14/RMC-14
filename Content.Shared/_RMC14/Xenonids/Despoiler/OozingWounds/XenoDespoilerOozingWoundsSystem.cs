using System.Numerics;
using Content.Shared._RMC14.Actions;
using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared._RMC14.Xenonids.Despoiler;
using Content.Shared._RMC14.Xenonids.Despoiler.Catalyze;
using Content.Shared._RMC14.Xenonids.Despoiler.Lingering;
using Content.Shared._RMC14.Xenonids.Despoiler.Spray;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.Damage;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Random;
using Robust.Shared.Spawners;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Xenonids.Despoiler.OozingWounds;

public sealed class XenoDespoilerOozingWoundsSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly MobThresholdSystem _mobThresholds = default!;
    [Dependency] private readonly SharedRMCActionsSystem _rmcActions = default!;
    [Dependency] private readonly SharedXenoHiveSystem _hive = default!;
    [Dependency] private readonly XenoDespoilerCatalyzeFlagSystem _catalyze = default!;

    private EntityQuery<XenoDespoilerAcidSprayComponent> _sprayQuery;
    private EntityQuery<XenoDespoilerLingeringAcidComponent> _lingeringQuery;

    public override void Initialize()
    {
        _sprayQuery = GetEntityQuery<XenoDespoilerAcidSprayComponent>();
        _lingeringQuery = GetEntityQuery<XenoDespoilerLingeringAcidComponent>();

        SubscribeLocalEvent<XenoDespoilerComponent, XenoDespoilerOozingWoundsActionEvent>(OnUse);
    }

    private void OnUse(Entity<XenoDespoilerComponent> ent, ref XenoDespoilerOozingWoundsActionEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp<XenoDespoilerOozingWoundsActionComponent>(args.Action, out var action))
            return;

        if (!_rmcActions.TryUseAction(args))
            return;

        var empowered = _catalyze.TakeEmpowerment(ent);

        var severity = ComputeSeverity(ent, action);
        var radius = action.BaseRadius + severity;
        var origin = Transform(ent).Coordinates;
        var sprayProto = empowered ? action.AcidSprayEmpoweredProto : action.AcidSprayProto;
        var now = _timing.CurTime;

        var pending = EnsureComp<XenoDespoilerOozingWoundsPendingComponent>(ent);

        for (var dx = -radius; dx <= radius; dx++)
        {
            for (var dy = -radius; dy <= radius; dy++)
            {
                if (dx == 0 && dy == 0)
                    continue;

                var euclid = MathF.Sqrt(dx * dx + dy * dy);
                if (euclid > radius + 0.001f)
                    continue;

                var cheby = Math.Max(Math.Abs(dx), Math.Abs(dy));
                var tile = origin.Offset(new Vector2(dx, dy)).SnapToGrid(EntityManager);
                var spawnDelay = action.DistanceDelayPerTile * cheby;

                var telegraph = PredictedSpawnAtPosition(action.TelegraphProto, tile);
                _hive.SetSameHive(ent.Owner, telegraph);

                if (spawnDelay > TimeSpan.Zero)
                    EnsureComp<TimedDespawnComponent>(telegraph).Lifetime = (float)spawnDelay.TotalSeconds;

                pending.Pending.Add(new XenoDespoilerOozingWoundsPendingTile
                {
                    SpawnAt = now + spawnDelay,
                    Tile = tile,
                    SprayProto = sprayProto,
                    PuddleProto = action.LingeringAcidProto,
                    PuddleChance = action.LingeringAcidChance,
                });
            }
        }

        if (action.CastSound is { } sound)
            _audio.PlayPredicted(sound, ent, ent);

        args.Handled = true;
    }

    private int ComputeSeverity(EntityUid uid, XenoDespoilerOozingWoundsActionComponent action)
    {
        if (!TryComp<DamageableComponent>(uid, out var dmg))
            return 0;

        if (!_mobThresholds.TryGetThresholdForState(uid, MobState.Dead, out var deadThreshold) ||
            deadThreshold <= 0)
        {
            return 0;
        }

        var hpFrac = 1f - Math.Clamp((float)(dmg.TotalDamage / deadThreshold.Value), 0f, 1f);

        var severity = 0;
        if (hpFrac <= action.SeverityHpThreshold)
            severity++;

        return severity;
    }

    public override void Update(float frameTime)
    {
        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<XenoDespoilerOozingWoundsPendingComponent>();
        while (query.MoveNext(out var caster, out var pending))
        {
            for (var i = pending.Pending.Count - 1; i >= 0; i--)
            {
                var entry = pending.Pending[i];
                if (now < entry.SpawnAt)
                    continue;

                pending.Pending.RemoveAt(i);

                if (TerminatingOrDeleted(caster))
                    continue;

                var spray = PredictedSpawnAtPosition(entry.SprayProto, entry.Tile);
                _hive.SetSameHive(caster, spray);

                if (_sprayQuery.TryComp(spray, out var sprayComp))
                {
                    sprayComp.Caster = caster;
                    Dirty(spray, sprayComp);
                }

                if (!_random.Prob(entry.PuddleChance))
                    continue;

                var puddle = PredictedSpawnAtPosition(entry.PuddleProto, entry.Tile);
                _hive.SetSameHive(caster, puddle);
                if (!_lingeringQuery.TryComp(puddle, out var puddleComp))
                    continue;

                puddleComp.Caster = caster;
                Dirty(puddle, puddleComp);
            }

            if (pending.Pending.Count == 0)
                RemCompDeferred<XenoDespoilerOozingWoundsPendingComponent>(caster);
        }
    }
}
