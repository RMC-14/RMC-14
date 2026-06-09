using Content.Server.Destructible;
using Content.Shared._RMC14.Atmos;
using Content.Shared._RMC14.Dropship;
using Content.Shared._RMC14.Explosion;
using Content.Shared._RMC14.Hijack;
using Content.Shared.Damage;
using Content.Shared.Explosion;
using Content.Shared.FixedPoint;
using Content.Shared.GameTicking;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._RMC14.Hijack;

/// <summary>
///     Applies shipwide randomized structural damage after a hijacked dropship lands.
///     Percentages are calculated from the currently existing targets on the landing map.
/// </summary>
public sealed class RMCHijackRandomDamageSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedRMCExplosionSystem _rmcExplosion = default!;
    [Dependency] private readonly SharedRMCFlammableSystem _rmcFlammable = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private static readonly EntProtoId BrokenPipe = "GasPipeBroken";
    private static readonly EntProtoId PipeFire = "RMCHijackPipeFire";
    private static readonly EntProtoId PipeExplosionWarning = "RMCHijackPipeExplosionWarning";
    private static readonly ProtoId<ExplosionPrototype> Explosion = "RMC";

    // These ranges intentionally select a small random subset of existing map targets instead of wiping categories.
    private const float WallMinPercent = 0.03f;
    private const float WallMaxPercent = 0.06f;
    private const float WindowMinPercent = 0.30f;
    private const float WindowMaxPercent = 0.50f;
    private const float WindoorMinPercent = 0.50f;
    private const float WindoorMaxPercent = 1.00f;
    private const float PipeInitialMinPercent = 0.03f;
    private const float PipeInitialMaxPercent = 0.05f;
    private const float PipeMinPercent = 0.01f;
    private const float PipeMaxPercent = 0.015f;

    private const float PipeExplosionTotal = 300f;
    private const float PipeExplosionSlope = 25f;
    private const float PipeExplosionMax = 20f;
    private const int PipeFireRange = 2;

    private readonly List<(EntityUid Uid, RMCHijackRandomDamageTargetComponent Comp)> _wallTargets = new();
    private readonly List<(EntityUid Uid, RMCHijackRandomDamageTargetComponent Comp)> _windowTargets = new();
    private readonly List<(EntityUid Uid, RMCHijackRandomDamageTargetComponent Comp)> _windoorTargets = new();

    public override void Initialize()
    {
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
        SubscribeLocalEvent<DropshipHijackLandedEvent>(OnDropshipHijackLanded);

        SubscribeLocalEvent<RMCHijackActivePipeComponent, ComponentRemove>(OnPipeRemove);
        SubscribeLocalEvent<RMCHijackActivePipeComponent, EntityTerminatingEvent>(OnPipeRemove);
        SubscribeLocalEvent<RMCHijackActivePipeComponent, AnchorStateChangedEvent>(OnPipeAnchorChanged);
    }

    private void OnRoundRestart(RoundRestartCleanupEvent ev)
    {
        _wallTargets.Clear();
        _windowTargets.Clear();
        _windoorTargets.Clear();
    }

    private void OnDropshipHijackLanded(ref DropshipHijackLandedEvent ev)
    {
        _wallTargets.Clear();
        _windowTargets.Clear();
        _windoorTargets.Clear();

        var map = EnsureComp<RMCHijackActiveMapComponent>(ev.Map);

        // Build pools from the landing map only; hijack damage must never spill into other maps.
        var query = EntityQueryEnumerator<RMCHijackRandomDamageTargetComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var comp, out var xform))
        {
            if (!comp.Enabled || xform.MapUid != ev.Map)
                continue;

            if (comp.Category == RMCHijackRandomDamageCategory.Pipe)
            {
                var pipe = EnsureComp<RMCHijackActivePipeComponent>(uid);
                pipe.Map = ev.Map;

                if (xform.Anchored)
                    map.Pipes.Add(uid);
            }

            // Structural hijack damage only uses prototypes with a normal damage/destructible flow.
            if (comp.Category != RMCHijackRandomDamageCategory.Pipe &&
                (!HasComp<DamageableComponent>(uid) || !HasComp<DestructibleComponent>(uid)))
            {
                continue;
            }

            switch (comp.Category)
            {
                case RMCHijackRandomDamageCategory.Wall:
                    _wallTargets.Add((uid, comp));
                    break;
                case RMCHijackRandomDamageCategory.Window:
                    _windowTargets.Add((uid, comp));
                    break;
                case RMCHijackRandomDamageCategory.Windoor:
                    _windoorTargets.Add((uid, comp));
                    break;
            }
        }

        ApplyRandomDamage(_wallTargets, WallMinPercent, WallMaxPercent);
        ApplyRandomDamage(_windowTargets, WindowMinPercent, WindowMaxPercent);
        ApplyRandomDamage(_windoorTargets, WindoorMinPercent, WindoorMaxPercent);

        DoPipeBarrage(ev.Map, PipeInitialMinPercent, PipeInitialMaxPercent);
    }

    private void OnPipeRemove<T>(Entity<RMCHijackActivePipeComponent> ent, ref T args)
    {
        if (TerminatingOrDeleted(ent.Comp.Map))
            return;

        if (TryComp(ent.Comp.Map, out RMCHijackActiveMapComponent? map))
            map.Pipes.Remove(ent);
    }

    private void OnPipeAnchorChanged(Entity<RMCHijackActivePipeComponent> ent, ref AnchorStateChangedEvent args)
    {
        if (TerminatingOrDeleted(ent.Comp.Map) ||
            !TryComp(ent.Comp.Map, out RMCHijackActiveMapComponent? map) ||
            args.Detaching)
        {
            return;
        }

        if (args.Anchored)
            map.Pipes.Add(ent);
        else
            map.Pipes.Remove(ent);
    }

    /// <summary>
    ///     Shuffles a target pool, selects a random percentage of it, and splits the selected targets
    ///     between damage-only and break outcomes.
    /// </summary>
    private void ApplyRandomDamage(
        List<(EntityUid Uid, RMCHijackRandomDamageTargetComponent Comp)> targets,
        float minPercent,
        float maxPercent)
    {
        var count = GetRandomCount(targets.Count, minPercent, maxPercent);
        if (count == 0)
            return;

        _random.Shuffle(targets);

        var breakCount = count / 2;
        if (count % 2 != 0 && _random.Prob(0.5f))
            breakCount++;

        for (var i = 0; i < count; i++)
        {
            var (uid, comp) = targets[i];
            ApplyOutcome(uid, comp, i < breakCount);
        }
    }

    /// <summary>
    ///     Applies either the damage-only or breaking damage target for a selected entity.
    /// </summary>
    private void ApplyOutcome(EntityUid uid, RMCHijackRandomDamageTargetComponent comp, bool shouldBreak)
    {
        if (Deleted(uid))
            return;

        var damage = shouldBreak ? comp.BreakDamage : comp.Damage;
        if (damage == null)
            return;

        ApplyDamageUpToTarget(uid, damage);
    }

    /// <summary>
    ///     Applies enough damage to reach the requested target total without stacking repeated hijack damage.
    /// </summary>
    private void ApplyDamageUpToTarget(EntityUid uid, DamageSpecifier targetDamage)
    {
        if (!TryComp(uid, out DamageableComponent? damageable))
            return;

        var targetTotal = targetDamage.GetTotal();
        if (targetTotal <= FixedPoint2.Zero || damageable.TotalDamage >= targetTotal)
            return;

        var remaining = targetTotal - damageable.TotalDamage;
        var damage = targetDamage * (remaining / targetTotal);

        _damageable.TryChangeDamage(uid, damage, true, damageable: damageable);
    }

    /// <summary>
    ///     Selects a random percentage of remaining pipe targets and schedules their delayed explosions.
    /// </summary>
    private void DoPipeBarrage(Entity<RMCHijackActiveMapComponent?> map, float minPercent = PipeMinPercent, float maxPercent = PipeMaxPercent)
    {
        if (!Resolve(map, ref map.Comp, false))
            return;

        var count = GetRandomCount(map.Comp.Pipes.Count, minPercent, maxPercent);
        if (count == 0)
        {
            RemCompDeferred<RMCHijackActiveMapComponent>(map);
            return;
        }

        while (count > 0 && map.Comp.Pipes.Count > 0)
        {
            count--;
            var pipe = _random.Pick(map.Comp.Pipes);
            map.Comp.Pipes.Remove(pipe);
            StartPipeWarning((map, map.Comp), pipe);
        }

        map.Comp.Next = _timing.CurTime + map.Comp.NextDelay;
    }

    /// <summary>
    ///     Telegraphs a pipe explosion before bursting it, matching CM-SS13's delayed hijack barrage.
    /// </summary>
    private void StartPipeWarning(Entity<RMCHijackActiveMapComponent> map, EntityUid pipe)
    {
        if (Deleted(pipe) ||
            !TryComp(pipe, out TransformComponent? xform) ||
            !xform.Anchored)
        {
            return;
        }

        Spawn(PipeExplosionWarning, _transform.GetMoverCoordinates(pipe, xform));
        _audio.PlayPvs(map.Comp.PipeHiss, pipe);

        map.Comp.Explode.Add(pipe);
        map.Comp.ExplodeAt = _timing.CurTime + map.Comp.ExplodeDelay;
    }

    /// <summary>
    ///     Explodes a warned pipe, starts local fire, and leaves a broken pipe visual in its place.
    /// </summary>
    private void BurstPipe(EntityUid pipe)
    {
        if (Deleted(pipe) ||
            !TryComp(pipe, out TransformComponent? xform) ||
            xform.MapUid == null ||
            !xform.Anchored)
        {
            return;
        }

        var coordinates = _transform.GetMoverCoordinates(pipe, xform);
        var mapCoordinates = _transform.GetMapCoordinates(pipe, xform);
        var rotation = xform.LocalRotation;

        _rmcExplosion.QueueExplosion(
            mapCoordinates,
            Explosion,
            PipeExplosionTotal,
            PipeExplosionSlope,
            PipeExplosionMax,
            pipe,
            canCreateVacuum: false);

        _rmcFlammable.SpawnFireDiamond(PipeFire, coordinates, PipeFireRange);

        Del(pipe);
        var brokenPipe = Spawn(BrokenPipe, coordinates);
        _transform.SetLocalRotation(brokenPipe, rotation);
    }

    /// <summary>
    ///     Returns at least one selected target when a non-empty pool exists.
    /// </summary>
    private int GetRandomCount(int poolCount, float minPercent, float maxPercent)
    {
        if (poolCount == 0)
            return 0;

        var percent = _random.NextFloat(minPercent, maxPercent);
        return Math.Clamp((int) MathF.Round(poolCount * percent), 1, poolCount);
    }

    public override void Update(float frameTime)
    {
        var time = _timing.CurTime;
        var query = EntityQueryEnumerator<RMCHijackActiveMapComponent>();
        while (query.MoveNext(out var uid, out var active))
        {
            if (active.ExplodeAt != null && time >= active.ExplodeAt.Value)
            {
                active.ExplodeAt = null;

                try
                {
                    foreach (var explode in active.Explode)
                    {
                        BurstPipe(explode);
                    }
                }
                finally
                {
                    active.Explode.Clear();
                }
            }

            if (time < active.Next)
                continue;

            DoPipeBarrage((uid, active));
        }
    }
}
