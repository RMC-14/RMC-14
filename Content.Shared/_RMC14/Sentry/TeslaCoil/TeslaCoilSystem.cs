using Content.Shared._RMC14.Line;
using Content.Shared._RMC14.Slow;
using Content.Shared._RMC14.Sentry;
using Content.Shared._RMC14.Stun;
using Content.Shared.Effects;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Stunnable;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Sentry.TeslaCoil;

public sealed class TeslaCoilSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly LineSystem _line = default!;
    [Dependency] private readonly SharedColorFlashEffectSystem _colorFlash = default!;
    [Dependency] private readonly RMCSizeStunSystem _sizeStun = default!;
    [Dependency] private readonly RMCSlowSystem _slow = default!;
    [Dependency] private readonly RMCDazedSystem _dazed = default!;
    [Dependency] private readonly SentrySystem _sentrySystem = default!;
    [Dependency] private readonly SharedSentryTargetingSystem _targeting = default!;


    private readonly HashSet<EntityUid> _potentialTargets = new();
    private readonly List<EntityUid> _validTargets = new();

    public override void Update(float frameTime)
    {
        if (_net.IsClient)
            return;

        var time = _timing.CurTime;
        var teslaQuery =
            EntityQueryEnumerator<RMCTeslaCoilComponent, SentryComponent, TransformComponent, SentryTargetingComponent>();
        while (teslaQuery.MoveNext(out var uid,
                   out var teslaComp,
                   out var sentryComp,
                   out var xform,
                   out var targetingComp))
        {
            if (sentryComp.Mode != SentryMode.On || !xform.Anchored)
                continue;

            if (time < teslaComp.LastFired + teslaComp.FireDelay)
                continue;

            _potentialTargets.Clear();
            _validTargets.Clear();

            _entityLookup.GetEntitiesInRange(xform.Coordinates, teslaComp.Range, _potentialTargets, LookupFlags.Uncontained);

            var currentTargets = 0;
            foreach (var targetUid in _potentialTargets)
            {
                if (currentTargets >= teslaComp.MaxTargets)
                    break;

                if (targetUid == uid)
                    continue;

                if (!_interaction.InRangeUnobstructed(uid, targetUid, teslaComp.Range, popup: false))
                    continue;

                if (!_targeting.IsValidTarget((uid, targetingComp), targetUid))
                    continue;

                var isValidTarget = false;

                if (TryComp<SentryComponent>(targetUid, out var targetSentry))
                {
                    if (targetSentry.Mode == SentryMode.On)
                        isValidTarget = true;
                }
                else if (TryComp<MobStateComponent>(targetUid, out var mobState) &&
                         _mobState.IsAlive(targetUid, mobState))
                {
                    isValidTarget = true;
                }

                if (isValidTarget)
                {
                    _validTargets.Add(targetUid);
                    currentTargets++;
                }
            }
            teslaComp.LastFired = time;

            if (_validTargets.Count > 0)
            {
                Dirty(uid, teslaComp);

                foreach (var target in _validTargets)
                {
                    ApplyTeslaEffects((uid, teslaComp), target);
                }
            }
        }
    }

    private void ApplyTeslaEffects(Entity<RMCTeslaCoilComponent> tesla, EntityUid target)
    {
        var teslaComp = tesla.Comp;

        if (TryComp<SentryComponent>(target, out var targetSentry) && targetSentry.Mode == SentryMode.On)
            _sentrySystem.TrySetMode((target, targetSentry), SentryMode.Off);
        else
        {
            if (teslaComp.StunDuration > TimeSpan.Zero)
            {
                if (_sizeStun.TryGetSize(target, out var size) && size <= RMCSizes.Xeno)
                    _stun.TryParalyze(target, teslaComp.StunDuration, true);
            }

            if (teslaComp.SlowDuration > TimeSpan.Zero)
                _slow.TrySuperSlowdown(target, teslaComp.SlowDuration);

            if (teslaComp.DazeDuration > TimeSpan.Zero)
                _dazed.TryDaze(target, teslaComp.DazeDuration, true);
        }

        _colorFlash.RaiseEffect(Color.Cyan,
            new List<EntityUid> { target },
            Filter.Pvs(target, entityManager: EntityManager));

        if (!string.IsNullOrEmpty(teslaComp.TeslaBeamProto))
            _line.TryCreateLine(tesla.Owner, target, teslaComp.TeslaBeamProto, out _);
    }
}
