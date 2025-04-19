using Content.Shared._RMC14.Actions;
using Content.Shared.Actions;
using Content.Shared.StatusEffect;

namespace Content.Shared._RMC14.Stun;

public sealed class RMCDazedSystem : EntitySystem
{
    [Dependency] private readonly StatusEffectsSystem _statusEffect = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCDazedComponent, DazedEvent>(OnDazed);
        SubscribeLocalEvent<RMCDazedComponent, ComponentShutdown>(OnDazedEnd);
    }

    /// <summary>
    ///     Put actions with the RMCDazeableActionComponent on cooldown for the given duration, only if the current
    ///     cooldown isn't higher already.
    /// </summary>
    /// <seealso cref="RMCDazeableActionComponent"/>
    private void OnDazed(Entity<RMCDazedComponent> ent, ref DazedEvent args)
    {
        foreach (var (actionId, _) in _actions.GetActions(ent))
        {
            if (TryComp(actionId, out RMCDazeableActionComponent? dazeable))
            {
                _actions.SetEnabled(actionId, false);
                _actions.SetCharges(actionId, 0);
            }
        }
    }

    private void OnDazedEnd(Entity<RMCDazedComponent> ent, ref ComponentShutdown args)
    {
        foreach (var (actionId, _) in _actions.GetActions(ent))
        {
            if (TryComp(actionId, out RMCDazeableActionComponent? dazeable))
            {
                _actions.SetEnabled(actionId, true);
                _actions.SetCharges(actionId, null);
            }
        }
    }

    public bool TryDaze(EntityUid uid, TimeSpan time, bool refresh = false, StatusEffectsComponent? status = null)
    {
        if (!Resolve(uid, ref status, false))
            return false;

        if (time <= TimeSpan.Zero)
            return false;

        if (HasComp<RMCDazedComponent>(uid) || !_statusEffect.TryAddStatusEffect<RMCDazedComponent>(uid, "Dazed", time, refresh))
            return false;

        var ev = new DazedEvent(time);
        RaiseLocalEvent(uid, ref ev);

        return true;
    }
}

/// <summary>
///     Raised directed on an entity when it is dazed.
/// </summary>
[ByRefEvent]
public record struct DazedEvent(TimeSpan Duration);
