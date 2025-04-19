using Content.Shared._RMC14.Actions;
using Content.Shared.Actions;
using Content.Shared.StatusEffect;

namespace Content.Shared.Stunnable;

public sealed class RMCDazedSystem : EntitySystem
{
    [Dependency] private readonly StatusEffectsSystem _statusEffect = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RMCDazedComponent, DazedEvent>(OnDazed);
    }

    /// <summary>
    ///     Put actions with the RMCDazeableActionComponent on cooldown for the given duration, only if the current
    ///     cooldown isn't higher already.
    /// </summary>
    /// <seealso cref="RMCDazeableActionComponent"/>
    private void OnDazed(EntityUid uid, RMCDazedComponent component, ref DazedEvent args)
    {
        foreach (var (actionId, _) in _actions.GetActions(uid))
        {
            if (TryComp(actionId, out RMCDazeableActionComponent? dazeable))
            {
                var newCooldownDuration = args.Duration * dazeable.DurationMultiplier;
                _actions.SetIfBiggerCooldown(actionId, newCooldownDuration);
            }
        }
    }

    public bool TryDaze(EntityUid uid, TimeSpan time, bool refresh = false, StatusEffectsComponent? status = null)
    {
        if (!Resolve(uid, ref status, false))
            return false;

        if (time <= TimeSpan.Zero)
            return false;

        if (_statusEffect.TryAddStatusEffect<RMCDazedComponent>(uid, "Dazed", time, refresh, status))
        {
            var ev = new DazedEvent(time);
            RaiseLocalEvent(uid, ref ev);
            return true;
        }

        return false;
    }
}

/// <summary>
///     Raised directed on an entity when it is dazed.
/// </summary>
[ByRefEvent]
public record struct DazedEvent(TimeSpan Duration);
