using Content.Shared._RMC14.Stun;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Projectiles;
using Content.Shared.Whitelist;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Effects.Buildup;

public sealed class RMCBuildupSystem : EntitySystem
{
    [Dependency] private readonly EntityWhitelistSystem _entityWhitelist = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly RMCSizeStunSystem _size = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private readonly List<ProtoId<RMCBuildupPrototype>> _remove = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RMCApplyBuildupOnHitComponent, ProjectileHitEvent>(OnProjectileHit);
        SubscribeLocalEvent<RMCBuildupComponent, EntityUnpausedEvent>(OnBuildupUnpaused);
    }

    private void OnProjectileHit(Entity<RMCApplyBuildupOnHitComponent> ent, ref ProjectileHitEvent args)
    {
        if (_net.IsClient)
            return;

        if (TryApply(args.Target, ent.Comp.Buildup, ent.Comp.Amount) != RMCBuildupApplyResult.Triggered)
            return;

        var triggered = new RMCBuildupTriggeredEvent(args.Target, ent.Comp.Buildup, args.Shooter);
        RaiseLocalEvent(ent, ref triggered);
    }

    private void OnBuildupUnpaused(Entity<RMCBuildupComponent> ent, ref EntityUnpausedEvent args)
    {
        foreach (var state in ent.Comp.States.Values)
        {
            if (state.NextDecayAt != null)
                state.NextDecayAt += args.PausedTime;
        }

        Dirty(ent);
    }

    public RMCBuildupApplyResult TryApply(
        EntityUid target,
        ProtoId<RMCBuildupPrototype> buildupId,
        int amount)
    {
        if (_net.IsClient || amount <= 0)
            return RMCBuildupApplyResult.None;

        var buildupPrototype = _prototype.Index(buildupId);
        if (!CanApply(target, buildupPrototype))
            return RMCBuildupApplyResult.None;

        var buildup = EnsureComp<RMCBuildupComponent>(target);
        var started = false;
        if (!buildup.States.TryGetValue(buildupId, out var state))
        {
            started = true;
            state = new RMCBuildupState
            {
                NextDecayAt = GetNextDecayAt(buildupPrototype),
            };
            buildup.States.Add(buildupId, state);

            if (buildupPrototype.AppliedPopup is { } appliedPopup)
            {
                _popup.PopupEntity(
                    Loc.GetString(appliedPopup),
                    target,
                    target,
                    buildupPrototype.AppliedPopupType);
            }
        }
        else if (buildupPrototype.RefreshDecayOnApply)
        {
            state.NextDecayAt = GetNextDecayAt(buildupPrototype);
        }

        state.Current += amount;
        if (state.Current < Math.Max(1, buildupPrototype.Threshold))
        {
            Dirty(target, buildup);
            return started ? RMCBuildupApplyResult.Started : RMCBuildupApplyResult.Applied;
        }

        buildup.States.Remove(buildupId);
        if (buildup.States.Count == 0)
            RemCompDeferred<RMCBuildupComponent>(target);
        else
            Dirty(target, buildup);

        if (buildupPrototype.TriggeredPopup is { } triggeredPopup)
        {
            _popup.PopupEntity(
                Loc.GetString(triggeredPopup),
                target,
                target,
                buildupPrototype.TriggeredPopupType);
        }

        return RMCBuildupApplyResult.Triggered;
    }

    private bool CanApply(EntityUid target, RMCBuildupPrototype buildup)
    {
        if (!_entityWhitelist.IsWhitelistPassOrNull(buildup.Whitelist, target) ||
            (!buildup.AffectsDead && _mobState.IsDead(target)))
        {
            return false;
        }

        if (buildup.MinimumSize == null && buildup.MaximumSize == null)
            return true;

        if (!_size.TryGetSize(target, out var size))
            return false;

        return (buildup.MinimumSize == null || size >= buildup.MinimumSize) &&
               (buildup.MaximumSize == null || size <= buildup.MaximumSize);
    }

    private TimeSpan? GetNextDecayAt(RMCBuildupPrototype buildup)
    {
        return buildup.DecayEvery > TimeSpan.Zero
            ? _timing.CurTime + buildup.DecayEvery
            : null;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_net.IsClient)
            return;

        var time = _timing.CurTime;
        var query = EntityQueryEnumerator<RMCBuildupComponent>();
        while (query.MoveNext(out var uid, out var buildup))
        {
            var changed = false;
            _remove.Clear();

            foreach (var (id, state) in buildup.States)
            {
                if (state.NextDecayAt is not { } nextDecayAt || time < nextDecayAt)
                    continue;

                if (!_prototype.TryIndex(id, out var buildupPrototype) ||
                    buildupPrototype.DecayAmount <= 0 ||
                    buildupPrototype.DecayEvery <= TimeSpan.Zero)
                {
                    state.NextDecayAt = null;
                    changed = true;
                    continue;
                }

                var elapsedPeriods = (int)((time - nextDecayAt).Ticks / buildupPrototype.DecayEvery.Ticks) + 1;
                state.Current -= elapsedPeriods * buildupPrototype.DecayAmount;
                changed = true;

                if (state.Current <= 0)
                {
                    _remove.Add(id);
                    continue;
                }

                state.NextDecayAt += buildupPrototype.DecayEvery * elapsedPeriods;
            }

            foreach (var id in _remove)
            {
                buildup.States.Remove(id);
            }

            if (buildup.States.Count == 0)
            {
                RemCompDeferred<RMCBuildupComponent>(uid);
                continue;
            }

            if (changed)
                Dirty(uid, buildup);
        }
    }
}
