using Content.Shared._RMC14.Actions;
using Content.Shared.Actions;
using Content.Shared.Charges.Components;
using Content.Shared.Charges.Systems;
using Content.Shared.Speech.EntitySystems;
using Content.Shared.StatusEffect;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Stun;

public sealed class RMCDazedSystem : EntitySystem
{
    [Dependency] private readonly SharedChargesSystem _charges = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffect = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly ISharedPlayerManager _playerManager = default!;
    [Dependency] private readonly SharedStutteringSystem _stutter = default!;

    public override void Initialize()
    {
        base.Initialize();

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
            if (TryComp(actionId, out RMCDazeableActionComponent? _))
            {
                _actions.SetEnabled(actionId, false);

                if (HasComp<LimitedChargesComponent>(actionId))
                    _charges.SetCharges(actionId, 0);
            }
        }
    }

    private void OnDazedEnd(Entity<RMCDazedComponent> ent, ref ComponentShutdown args)
    {
        foreach (var (actionId, _) in _actions.GetActions(ent))
        {
            if (TryComp(actionId, out RMCDazeableActionComponent? _))
            {
                _actions.SetEnabled(actionId, true);
                _charges.ResetCharges(actionId);
            }
        }

        if (_net.IsServer && _playerManager.TryGetSessionByEntity(ent.Owner, out var session))
        {
            var ev = new DazedComponentShutdownEvent();
            RaiseNetworkEvent(ev, session.Channel);
        }
    }

    public bool TryDaze(EntityUid uid, TimeSpan time, bool refresh = false, StatusEffectsComponent? status = null, bool stutter = false)
    {
        if (!Resolve(uid, ref status, false))
            return false;

        if (time <= TimeSpan.Zero)
            return false;

        if (_statusEffect.TryAddStatusEffect<RMCDazedComponent>(uid, "Dazed", time, refresh, status))
        {
            if (stutter)
                _stutter.DoStutter(uid, time, true);

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

[NetSerializable, Serializable]
public sealed class DazedComponentShutdownEvent: EntityEventArgs;
