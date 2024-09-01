using Content.Shared.Coordinates;
using Content.Shared.Movement.Systems;
using Content.Shared.Standing;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Xenonids.MeleeSlow;

public sealed class XenoMeleeSlowSystem : EntitySystem
{
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeed = default!;
    [Dependency] private readonly StandingStateSystem _standing = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly XenoSystem _xeno = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<XenoMeleeSlowComponent, MeleeHitEvent>(OnHit);

        SubscribeLocalEvent<XenoSlowedComponent, RefreshMovementSpeedModifiersEvent>(OnXenoSlowRefreshSpeed);
        SubscribeLocalEvent<XenoSlowedComponent, ComponentRemove>(OnXenoSlowRemoved);
    }

    private void OnHit(Entity<XenoMeleeSlowComponent> xeno, ref MeleeHitEvent args)
    {
        if (!args.IsHit || args.HitEntities.Count == 0)
            return;

        foreach (var entity in args.HitEntities)
        {
            if (!_xeno.CanAbilityAttackTarget(xeno, entity))
                return;

            if (xeno.Comp.RequiresKnockDown && !_standing.IsDown(entity))
                return;

            var victim = EnsureComp<XenoSlowedComponent>(entity);

            victim.ExpiresAt = _timing.CurTime + xeno.Comp.SlowTime;
            victim.SpeedMultiplier = xeno.Comp.SpeedMultiplier;

            _movementSpeed.RefreshMovementSpeedModifiers(entity);

            if (_net.IsServer)
            {
                if (victim.Effect != null)
                    QueueDel(victim.Effect);
                victim.Effect = SpawnAttachedTo(xeno.Comp.Effect, entity.ToCoordinates());
            }

            Dirty(entity, victim);

            break;
        }
    }

    private void OnXenoSlowRefreshSpeed(Entity<XenoSlowedComponent> victim, ref RefreshMovementSpeedModifiersEvent args)
    {
        var multiplier = victim.Comp.SpeedMultiplier.Float();
        args.ModifySpeed(multiplier, multiplier);
    }

    private void OnXenoSlowRemoved(Entity<XenoSlowedComponent> victim, ref ComponentRemove args)
    {
        if (!TerminatingOrDeleted(victim))
            _movementSpeed.RefreshMovementSpeedModifiers(victim);
    }

    public override void Update(float frameTime)
    {
        var time = _timing.CurTime;

        var victimQuery = EntityQueryEnumerator<XenoSlowedComponent>();
        while (victimQuery.MoveNext(out var uid, out var victim))
        {
            if (victim.ExpiresAt > time)
                continue;

            RemCompDeferred<XenoMeleeSlowComponent>(uid);
            _movementSpeed.RefreshMovementSpeedModifiers(uid);
        }
    }
}
