using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Maths;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Weapons.Ranged;

public sealed class GunSpinupSystem : EntitySystem
{
    private const float ClientWindupSafetyPadding = 0.05f;
    private const float ModifierRefreshEpsilon = 0.01f;

    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedGunSystem _gun = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<GunSpinupComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<GunSpinupComponent, AttemptShootEvent>(OnAttemptShoot);
        SubscribeLocalEvent<GunSpinupComponent, GunShotEvent>(OnGunShot);
        SubscribeLocalEvent<GunSpinupComponent, GunRefreshModifiersEvent>(OnRefreshModifiers);
    }

    private void OnStartup(Entity<GunSpinupComponent> ent, ref ComponentStartup args)
    {
        ent.Comp.LastUpdate = _timing.CurTime;
        ent.Comp.CurrentSpinLevel = ent.Comp.MinSpinLevel;
        ent.Comp.LastAppliedRate = -1f;
        ent.Comp.LastAppliedScatter = -1f;
        ent.Comp.WasFiring = false;
        ent.Comp.StartSoundPlayed = false;
        ent.Comp.LastAttemptAt = null;
        ent.Comp.PendingWindupUntil = null;
        ent.Comp.LastLoopSoundAt = null;

        if (TryComp(ent, out GunComponent? gun))
            _gun.RefreshModifiers((ent, gun));
    }

    private void OnAttemptShoot(Entity<GunSpinupComponent> ent, ref AttemptShootEvent args)
    {
        if (_net.IsClient && !_timing.IsFirstTimePredicted)
            return;

        if (args.Cancelled)
            return;

        if (ent.Comp.InitialWindupDelay <= 0f)
            return;

        var now = _timing.CurTime;
        if (IsSpinActive(ent.Comp, now))
            return;

        if (ent.Comp.PendingWindupUntil == null &&
            ent.Comp.LastAttemptAt is { } lastAttempt &&
            (now - lastAttempt).TotalSeconds > ent.Comp.InitialWindupResetGap)
        {
            ent.Comp.PendingWindupUntil = null;
            ent.Comp.StartSoundPlayed = false;
        }

        ent.Comp.LastAttemptAt = now;

        if (ent.Comp.PendingWindupUntil is not { } pendingUntil)
        {
            pendingUntil = now + TimeSpan.FromSeconds(ent.Comp.InitialWindupDelay);
            ent.Comp.PendingWindupUntil = pendingUntil;

            if (!ent.Comp.StartSoundPlayed && ent.Comp.StartSound != null)
            {
                _audio.PlayPredicted(ent.Comp.StartSound, ent, args.User);
                ent.Comp.StartSoundPlayed = true;
            }
        }

        var readyAt = pendingUntil + (_net.IsClient
            ? TimeSpan.FromSeconds(ClientWindupSafetyPadding)
            : TimeSpan.Zero);

        if (now >= readyAt)
        {
            ent.Comp.PendingWindupUntil = null;
            return;
        }

        args.Cancelled = true;
        args.ResetCooldown = true;
    }

    private void OnGunShot(Entity<GunSpinupComponent> ent, ref GunShotEvent args)
    {
        var now = _timing.CurTime;
        var wasSpunUp = IsSpinActive(ent.Comp, now);
        ent.Comp.LastShotAt = now;
        ent.Comp.PendingWindupUntil = null;

        if (!wasSpunUp && ent.Comp.StartSound != null && !ent.Comp.StartSoundPlayed)
            _audio.PlayPredicted(ent.Comp.StartSound, ent, args.User);

        if (ent.Comp.LoopSound != null &&
            (ent.Comp.LastLoopSoundAt == null ||
             (now - ent.Comp.LastLoopSoundAt.Value).TotalSeconds >= MathF.Max(ent.Comp.LoopSoundCooldown, 0f)))
        {
            _audio.PlayPredicted(ent.Comp.LoopSound, ent, args.User);
            ent.Comp.LastLoopSoundAt = now;
        }

        ent.Comp.StartSoundPlayed = true;
    }

    private void OnRefreshModifiers(Entity<GunSpinupComponent> ent, ref GunRefreshModifiersEvent args)
    {
        var rate = GetRateMultiplier(ent.Comp);
        var finalFireRate = rate / MathF.Max(ent.Comp.BaseShotDelay, 0.01f);
        var scatter = ent.Comp.BaseScatter / MathF.Max(rate, 1);
        var scatterAngle = Angle.FromDegrees(MathF.Max(scatter, 0f));

        args.FireRate = finalFireRate;
        args.MinAngle = scatterAngle;
        args.MaxAngle = scatterAngle;
        args.AngleIncrease = Angle.Zero;
        args.AngleDecay = Angle.Zero;
    }

    public override void Update(float frameTime)
    {
        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<GunSpinupComponent, GunComponent>();

        while (query.MoveNext(out var uid, out var spin, out var gun))
        {
            var dt = (float) (now - spin.LastUpdate).TotalSeconds;
            if (dt <= 0f)
                continue;

            spin.LastUpdate = now;
            var isFiring = IsFiring(spin, now);
            var isSpinActive = IsSpinActive(spin, now);

            if (spin.WasFiring && !isSpinActive && spin.StopSound != null)
                _audio.PlayPvs(spin.StopSound, uid);

            if (spin.WasFiring && !isSpinActive)
                spin.StartSoundPlayed = false;

            spin.WasFiring = isSpinActive;

            var spinRange = MathF.Max(spin.MaxSpinLevel - spin.MinSpinLevel, 0f);
            if (spinRange <= 0f)
                continue;

            var level = Math.Clamp(spin.CurrentSpinLevel, spin.MinSpinLevel, spin.MaxSpinLevel);

            if (isFiring)
            {
                var up = spinRange / MathF.Max(spin.SpinUpTime, 0.01f);
                level += up * dt;
            }
            else if (!isSpinActive)
            {
                var down = spinRange / MathF.Max(spin.SpinDownTime, 0.01f);
                level -= down * dt;
            }

            level = Math.Clamp(level, spin.MinSpinLevel, spin.MaxSpinLevel);
            spin.CurrentSpinLevel = level;

            var newRate = GetRateMultiplier(spin, level);
            var newScatter = spin.BaseScatter / MathF.Max(newRate, 1f);

            if (Math.Abs(newRate - spin.LastAppliedRate) < ModifierRefreshEpsilon &&
                Math.Abs(newScatter - spin.LastAppliedScatter) < ModifierRefreshEpsilon)
                continue;

            spin.LastAppliedRate = newRate;
            spin.LastAppliedScatter = newScatter;
            _gun.RefreshModifiers((uid, gun));
        }
    }

    private static bool IsFiring(GunSpinupComponent comp, TimeSpan now)
    {
        if (comp.LastShotAt is not { } lastShot)
            return false;

        var currentRate = GetRateMultiplier(comp, comp.CurrentSpinLevel);
        var expectedDelay = comp.BaseShotDelay / MathF.Max(currentRate, 1);
        var fireWindow = expectedDelay + MathF.Max(comp.FireWindowPadding, 0f);
        return (now - lastShot).TotalSeconds <= fireWindow;
    }

    private static bool IsSpinActive(GunSpinupComponent comp, TimeSpan now)
    {
        if (comp.LastShotAt is not { } lastShot)
            return false;

        return (now - lastShot).TotalSeconds <= MathF.Max(comp.GraceAfterStop, 0f);
    }

    private static float GetRateMultiplier(GunSpinupComponent comp)
    {
        return GetRateMultiplier(comp, comp.CurrentSpinLevel);
    }

    private static float GetRateMultiplier(GunSpinupComponent comp, float level)
    {
        if (comp.RateTiers.Length == 0)
            return 1f;

        if (comp.RateTiers.Length == 1)
            return MathF.Max(comp.RateTiers[0], 1);

        var clamped = Math.Clamp(level, 1f, comp.RateTiers.Length);
        var lowerIndex = Math.Clamp((int) MathF.Floor(clamped) - 1, 0, comp.RateTiers.Length - 1);
        var upperIndex = Math.Min(lowerIndex + 1, comp.RateTiers.Length - 1);
        var localT = clamped - (lowerIndex + 1);

        var lower = MathF.Max(comp.RateTiers[lowerIndex], 1);
        var upper = MathF.Max(comp.RateTiers[upperIndex], 1);
        return lower + (upper - lower) * localT;
    }
}
