using Content.Shared._RMC14.Movement;
using Content.Shared._RMC14.Stamina;
using Content.Shared._RMC14.Xenonids;
using Content.Shared.Movement.Systems;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Chemistry.Buildup;

public sealed class RMCSpeedBuildupSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _speed = default!;
    [Dependency] private readonly RMCStaminaSystem _stamina = default!;
    [Dependency] private readonly TemporarySpeedModifiersSystem _temporarySpeed = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private const float SlowFactor = 0.075f;
    private const float BoostFactor = 0.1f;

    // cm13 HUMAN_STAMINA_MULTIPLIER
    private const float StaminaMultiplier = 5f;

    private static readonly TimeSpan UpdateInterval = TimeSpan.FromSeconds(1);
    private TimeSpan _nextUpdate;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RMCSpeedBuildupComponent, RMCLateRefreshMovementSpeedModifiersEvent>(OnLateRefreshMovement);
    }

    private void OnLateRefreshMovement(Entity<RMCSpeedBuildupComponent> ent, ref RMCLateRefreshMovementSpeedModifiersEvent args)
    {
        if (!HasComp<XenoComponent>(ent))
            return;

        var raw = ent.Comp.IncreaseSpeed ? -ent.Comp.Magnitude * BoostFactor : ent.Comp.Magnitude * SlowFactor;
        if (_temporarySpeed.CalculateLateSpeedModifier(ent, args.Modifiers.SprintSpeedModifier, raw) is not { } multiplier)
            return;

        args.Modifiers.ModifySpeed(multiplier, multiplier);
    }

    public void AddBuildup(EntityUid uid, float magnitude, float dissipationRate, float maxBuildup, bool increaseSpeed)
    {
        if (_net.IsClient)
            return;

        if (TryComp(uid, out RMCSpeedBuildupComponent? comp) && comp.Magnitude > 0f)
        {
            comp.Magnitude = Math.Min(comp.Magnitude + magnitude, comp.MaxBuildup);
        }
        else
        {
            comp = EnsureComp<RMCSpeedBuildupComponent>(uid);
            comp.Magnitude = magnitude;
            comp.DissipationRate = dissipationRate;
            comp.MaxBuildup = maxBuildup;
            comp.IncreaseSpeed = increaseSpeed;
        }

        Dirty(uid, comp);
        _speed.RefreshMovementSpeedModifiers(uid);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_net.IsClient)
            return;

        var time = _timing.CurTime;
        if (time < _nextUpdate)
            return;

        _nextUpdate = time + UpdateInterval;
        var delta = (float) UpdateInterval.TotalSeconds;

        var query = EntityQueryEnumerator<RMCSpeedBuildupComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            comp.Magnitude = MathF.Max(comp.Magnitude - comp.DissipationRate * delta, 0f);

            if (TryComp<RMCStaminaComponent>(uid, out var stamina))
            {
                var amount = StaminaMultiplier * comp.DissipationRate * delta;
                _stamina.DoStaminaDamage((uid, stamina), comp.IncreaseSpeed ? -amount : amount, false);
            }

            if (comp.Magnitude <= 0f)
            {
                RemCompDeferred<RMCSpeedBuildupComponent>(uid);
                _speed.RefreshMovementSpeedModifiers(uid);
                continue;
            }

            Dirty(uid, comp);
            _speed.RefreshMovementSpeedModifiers(uid);
        }
    }
}
