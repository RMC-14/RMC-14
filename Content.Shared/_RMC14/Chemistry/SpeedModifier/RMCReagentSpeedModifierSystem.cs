using Content.Shared._RMC14.Movement;
using Content.Shared._RMC14.Xenonids;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Chemistry.SpeedModifier;

public sealed class RMCReagentSpeedModifierSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _speed = default!;
    [Dependency] private readonly TemporarySpeedModifiersSystem _temporarySpeed = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private static readonly TimeSpan Lifetime = TimeSpan.FromSeconds(2);

    private const float ReducibleFloor = 0.1f;

    private readonly List<string> _expired = new();

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCReagentSpeedModifierComponent, RMCLateRefreshMovementSpeedModifiersEvent>(OnLateRefreshMovement);
    }

    private void OnLateRefreshMovement(Entity<RMCReagentSpeedModifierComponent> ent, ref RMCLateRefreshMovementSpeedModifiersEvent args)
    {
        var delay = 0f;
        foreach (var value in ent.Comp.Delays.Values)
        {
            delay += value;
        }

        if (delay == 0f || !TryComp(ent, out MovementSpeedModifierComponent? movement))
            return;

        var currentModifier = args.Modifiers.SprintSpeedModifier;
        if (delay < 0f && movement.BaseSprintSpeed > 0f && currentModifier > 0f)
        {
            var baseDelay = 10 / movement.BaseSprintSpeed;
            var slowdown = Math.Max(baseDelay / currentModifier - baseDelay, 0f);
            delay = Math.Max(delay, -ReducibleFloor - slowdown);
        }

        if (_temporarySpeed.CalculateLateSpeedModifier(ent, currentModifier, delay, movement) is not { } multiplier)
            return;

        args.Modifiers.ModifySpeed(multiplier, multiplier);
    }

    public void SetSpeedModifier(EntityUid uid, string key, float delay)
    {
        if (_net.IsClient || HasComp<XenoComponent>(uid))
            return;

        var comp = EnsureComp<RMCReagentSpeedModifierComponent>(uid);
        comp.Delays[key] = delay;
        comp.ExpiresAt[key] = _timing.CurTime + Lifetime;
        Dirty(uid, comp);
        _speed.RefreshMovementSpeedModifiers(uid);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_net.IsClient)
            return;

        var time = _timing.CurTime;
        var query = EntityQueryEnumerator<RMCReagentSpeedModifierComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            _expired.Clear();
            foreach (var (key, expiresAt) in comp.ExpiresAt)
            {
                if (expiresAt <= time)
                    _expired.Add(key);
            }

            if (_expired.Count == 0)
                continue;

            foreach (var key in _expired)
            {
                comp.Delays.Remove(key);
                comp.ExpiresAt.Remove(key);
            }

            if (comp.Delays.Count == 0)
                RemCompDeferred<RMCReagentSpeedModifierComponent>(uid);
            else
                Dirty(uid, comp);

            _speed.RefreshMovementSpeedModifiers(uid);
        }
    }
}
