using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Movement;

public sealed class TemporarySpeedModifiersSystem : EntitySystem
{
    private const float MaxSpeedModifier = 1f;

    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeedSystem = default!;
    [Dependency] private readonly INetManager _netManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<TemporarySpeedModifiersComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMovement);
    }

    private void OnRefreshMovement(Entity<TemporarySpeedModifiersComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        foreach (var modifier in ent.Comp.Modifiers)
        {
            if (modifier.ExpiresAt <= _timing.CurTime)
                continue;

            args.ModifySpeed(modifier.Walk, modifier.Sprint);
        }
    }

    public override void Update(float frameTime)
    {
        var time = _timing.CurTime;
        var speedModsQuery = EntityQueryEnumerator<TemporarySpeedModifiersComponent>();

        while (speedModsQuery.MoveNext(out var uid, out var speedModsComponent))
        {
            var toRemove = new List<(TimeSpan ExpiresAt, float Walk, float Sprint)>();

            foreach (var modifier in speedModsComponent.Modifiers)
            {
                if (modifier.ExpiresAt > time)
                    continue;

                toRemove.Add(modifier);
            }

            foreach (var modifier in toRemove)
            {
                speedModsComponent.Modifiers.Remove(modifier);
            }

            if (toRemove.Count > 0)
                Dirty(uid, speedModsComponent);

            if (speedModsComponent.Modifiers.Count <= 0)
                RemCompDeferred<TemporarySpeedModifiersComponent>(uid);

            _movementSpeedSystem.RefreshMovementSpeedModifiers(uid);
        }
    }

    public void ModifySpeed(EntityUid entUid, List<TemporarySpeedModifierSet> modifiers)
    {
        if (_netManager.IsClient)
            return;

        var comp = EnsureComp<TemporarySpeedModifiersComponent>(entUid);

        foreach (var modifier in modifiers)
        {
            comp.Modifiers.Add((_timing.CurTime + modifier.Duration, modifier.Walk, modifier.Sprint));
        }
    }

    /// <summary>
    ///     Calculate the multiplier to use for the speed modifier, the modifier entered should be the same as the one in CM13..
    /// </summary>
    /// <param name="uid">The entity whose speed is being modified.</param>
    /// <param name="modifier">The speed modifier value, positive values are a slow, negative values are a buff</param>
    /// <param name="movement">The <see cref="MovementSpeedModifierComponent"/> belonging to the entity</param>
    /// <returns>Null or a float representing the calculated speed multiplier</returns>
    public float? CalculateSpeedModifier(EntityUid uid, float modifier, MovementSpeedModifierComponent? movement = null)
    {
        if (!Resolve(uid, ref movement))
            return null;

        return 1 / (Math.Max(1 / movement.CurrentSprintSpeed * 10 + modifier, MaxSpeedModifier) / 10) / movement.CurrentSprintSpeed;
    }
}
