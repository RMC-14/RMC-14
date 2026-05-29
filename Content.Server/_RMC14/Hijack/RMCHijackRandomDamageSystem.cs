using Content.Shared._RMC14.Atmos;
using Content.Shared._RMC14.Dropship;
using Content.Shared._RMC14.Explosion;
using Content.Shared._RMC14.Hijack;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._RMC14.Hijack;

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
    private static readonly SoundSpecifier PipeHiss = new SoundPathSpecifier("/Audio/Ambience/Objects/gas_hiss.ogg");

    private static readonly TimeSpan PipeBarrageInterval = TimeSpan.FromSeconds(15);
    private static readonly TimeSpan PipeWarningDelay = TimeSpan.FromSeconds(5);

    private const float WallMinPercent = 0.03f;
    private const float WallMaxPercent = 0.06f;
    private const float WindowMinPercent = 0.30f;
    private const float WindowMaxPercent = 0.50f;
    private const float WindoorMinPercent = 0.50f;
    private const float WindoorMaxPercent = 1.00f;
    private const float PipeInitialPercent = 0.10f;
    private const float PipeMinPercent = 0.007f;
    private const float PipeMaxPercent = 0.012f;

    private const float PipeExplosionTotal = 300f;
    private const float PipeExplosionSlope = 25f;
    private const float PipeExplosionMax = 20f;
    private const int PipeFireRange = 2;

    private readonly List<(EntityUid Uid, RMCHijackRandomDamageTargetComponent Comp)> _wallTargets = new();
    private readonly List<(EntityUid Uid, RMCHijackRandomDamageTargetComponent Comp)> _windowTargets = new();
    private readonly List<(EntityUid Uid, RMCHijackRandomDamageTargetComponent Comp)> _windoorTargets = new();
    private readonly List<EntityUid> _pipeTargets = new();
    private readonly List<EntityUid> _barrageMaps = new();
    private readonly Dictionary<EntityUid, TimeSpan> _nextPipeBarrage = new();
    private readonly HashSet<EntityUid> _applyingHijackDamage = new();
    private readonly HashSet<EntityUid> _usedPipeTargets = new();
    private readonly HashSet<EntityUid> _pendingPipeTargets = new();

    public override void Initialize()
    {
        SubscribeLocalEvent<DropshipHijackLandedEvent>(OnDropshipHijackLanded);
        SubscribeLocalEvent<RMCHijackRandomDamageTargetComponent, BeforeDamageChangedEvent>(OnBeforeDamageChanged);
    }

    public override void Update(float frameTime)
    {
        if (_nextPipeBarrage.Count == 0)
            return;

        var now = _timing.CurTime;
        _barrageMaps.Clear();

        foreach (var (map, nextBarrage) in _nextPipeBarrage)
        {
            if (nextBarrage <= now)
                _barrageMaps.Add(map);
        }

        foreach (var map in _barrageMaps)
        {
            if (Deleted(map))
            {
                _nextPipeBarrage.Remove(map);
                continue;
            }

            DoPipeBarrage(map);
        }
    }

    private void OnDropshipHijackLanded(ref DropshipHijackLandedEvent ev)
    {
        _wallTargets.Clear();
        _windowTargets.Clear();
        _windoorTargets.Clear();

        var query = EntityQueryEnumerator<RMCHijackRandomDamageTargetComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var comp, out var xform))
        {
            if (!comp.Enabled || xform.MapUid != ev.Map)
                continue;

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

        DoPipeBarrage(ev.Map, PipeInitialPercent, PipeInitialPercent);
    }

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

    private void ApplyOutcome(EntityUid uid, RMCHijackRandomDamageTargetComponent comp, bool shouldBreak)
    {
        if (Deleted(uid))
            return;

        var damage = shouldBreak ? comp.BreakDamage : comp.Damage;
        if (damage == null)
            return;

        ApplyDamageUpToTarget(uid, damage);
    }

    private void ApplyDamageUpToTarget(EntityUid uid, DamageSpecifier targetDamage)
    {
        if (!TryComp(uid, out DamageableComponent? damageable))
            return;

        var targetTotal = targetDamage.GetTotal();
        if (targetTotal <= FixedPoint2.Zero || damageable.TotalDamage >= targetTotal)
            return;

        var remaining = targetTotal - damageable.TotalDamage;
        var damage = targetDamage * (remaining / targetTotal);

        _applyingHijackDamage.Add(uid);
        try
        {
            _damageable.TryChangeDamage(uid, damage, true, damageable: damageable);
        }
        finally
        {
            _applyingHijackDamage.Remove(uid);
        }
    }

    private void OnBeforeDamageChanged(Entity<RMCHijackRandomDamageTargetComponent> ent, ref BeforeDamageChangedEvent args)
    {
        if (!ent.Comp.HijackDamageOnly ||
            _applyingHijackDamage.Contains(ent.Owner) ||
            !args.Damage.AnyPositive())
        {
            return;
        }

        args.Cancelled = true;
    }

    private void DoPipeBarrage(EntityUid map, float minPercent = PipeMinPercent, float maxPercent = PipeMaxPercent)
    {
        _pipeTargets.Clear();

        var query = EntityQueryEnumerator<RMCHijackRandomDamageTargetComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var comp, out var xform))
        {
            if (!comp.Enabled ||
                comp.Category != RMCHijackRandomDamageCategory.Pipe ||
                xform.MapUid != map ||
                _usedPipeTargets.Contains(uid) ||
                _pendingPipeTargets.Contains(uid))
            {
                continue;
            }

            _pipeTargets.Add(uid);
        }

        var count = GetRandomCount(_pipeTargets.Count, minPercent, maxPercent);
        if (count == 0)
        {
            _nextPipeBarrage.Remove(map);
            return;
        }

        _random.Shuffle(_pipeTargets);

        for (var i = 0; i < count; i++)
            StartPipeWarning(_pipeTargets[i]);

        _nextPipeBarrage[map] = _timing.CurTime + PipeBarrageInterval;
    }

    private void StartPipeWarning(EntityUid pipe)
    {
        if (Deleted(pipe) || !TryComp(pipe, out TransformComponent? xform) || !_usedPipeTargets.Add(pipe))
            return;

        _pendingPipeTargets.Add(pipe);
        Spawn(PipeExplosionWarning, _transform.GetMoverCoordinates(pipe, xform));
        _audio.PlayPvs(PipeHiss, pipe);

        Timer.Spawn(PipeWarningDelay, () => BurstPipe(pipe));
    }

    private void BurstPipe(EntityUid pipe)
    {
        _pendingPipeTargets.Remove(pipe);

        if (Deleted(pipe) || !TryComp(pipe, out TransformComponent? xform) || xform.MapUid == null)
            return;

        var coordinates = _transform.GetMoverCoordinates(pipe, xform);
        var mapCoordinates = _transform.GetMapCoordinates(pipe, xform);
        var rotation = xform.LocalRotation;

        _rmcExplosion.QueueExplosion(
            mapCoordinates,
            "RMC",
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

    private int GetRandomCount(int poolCount, float minPercent, float maxPercent)
    {
        if (poolCount == 0)
            return 0;

        var percent = _random.NextFloat(minPercent, maxPercent);
        return Math.Clamp((int) MathF.Round(poolCount * percent), 1, poolCount);
    }
}
