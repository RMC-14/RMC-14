using Content.Shared._RMC14.Slow;
using Content.Shared.Clothing;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Movement;

public sealed class TemporarySpeedModifiersSystem : EntitySystem
{
    private const float MaxSpeedModifier = 1f;

    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeedSystem = default!;
    [Dependency] private readonly INetManager _netManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<TemporarySpeedModifiersComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMovement);

        SubscribeLocalEvent<ClothingSpeedModifierComponent, RMCMovementSpeedRefreshedEvent>(OnRMCRefreshMovement);
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

    private void OnRMCRefreshMovement(Entity<ClothingSpeedModifierComponent> ent, ref RMCMovementSpeedRefreshedEvent args)
    {
        if (!_container.TryGetContainingContainer((ent, null, null), out var wearer))
            return;

        if (!TryComp(wearer.Owner, out MovementSpeedModifierComponent? movement))
            return;

        if (!HasComp<RMCSlowdownComponent>(wearer.Owner) && !HasComp<RMCSuperSlowdownComponent>(wearer.Owner))
            return;

        // Modify the slowdown the clothing applies based on the wearers current movement speed.
        // Slowed entities get a reduced slow effect from clothing while fast entities get an increased slow effect.
        var newModifier = 1 - (1 - args.SprintModifier) * (movement.CurrentSprintSpeed / movement.BaseSprintSpeed);
        args.WalkModifier = newModifier;
        args.SprintModifier = newModifier;
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
    ///     Calculate the multiplier to use for the speed modifier.
    /// </summary>
    /// <param name="uid">The entity whose speed is being modified.</param>
    /// <param name="modifier">The speed modifier value, positive values are a slow, negative values are a buff</param>
    /// <param name="movement">The <see cref="MovementSpeedModifierComponent"/> belonging to the entity</param>
    /// <returns>Null or a float representing the calculated speed multiplier</returns>
    public float? CalculateSpeedModifier(EntityUid uid, float modifier, MovementSpeedModifierComponent? movement = null)
    {
        if (!Resolve(uid, ref movement))
            return null;

        var currentSpeed = movement.CurrentSprintSpeed;
        var baseSpeed = movement.BaseSprintSpeed;

        var currentSpeedModifier = 1 / (Math.Max(1 / currentSpeed * 10 + modifier, MaxSpeedModifier) / 10) / movement.CurrentSprintSpeed;
        var baseSpeedModifier = 1 / (Math.Max(1 / baseSpeed * 10 + modifier, MaxSpeedModifier) / 10) / movement.BaseSprintSpeed;

        // Apply the lowest of the two values.
        return Math.Min(currentSpeedModifier, baseSpeedModifier);
    }
}

[ByRefEvent]
public record struct RMCMovementSpeedRefreshedEvent(float WalkModifier, float SprintModifier);
