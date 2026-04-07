using Content.Shared._RMC14.Animations;
using Content.Shared._RMC14.Marines;
using Content.Shared._RMC14.Xenonids.Construction.Nest;
using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared._RMC14.Xenonids.Spray;
using Content.Shared.Atmos.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Standing;
using Content.Shared.Stunnable;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Xenonids.Construction.AcidPillar;

public sealed class AcidPillarSystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly SharedXenoHiveSystem _hive = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedRMCAnimationSystem _rmcAnimation = default!;
    [Dependency] private readonly StandingStateSystem _standingState = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly XenoSprayAcidSystem _xenoSprayAcid = default!;

    private readonly HashSet<Entity<MarineComponent>> _marines = new();
    private readonly HashSet<Entity<XenoComponent>> _xenos = new();

    private bool CanTarget(EntityUid pillar, EntityUid target)
    {
        if (_mobState.IsDead(target) || HasComp<XenoNestedComponent>(target))
            return false;

        if (_hive.FromSameHiveOrAlly(pillar, target))
        {
            return HasComp<XenoComponent>(target) &&
                   TryComp(target, out FlammableComponent? flammable) &&
                   flammable.OnFire;
        }

        return !_mobState.IsIncapacitated(target) &&
               !_standingState.IsDown(target) &&
               !HasComp<StunnedComponent>(target);
    }

    private void TrySetIfCloserTarget(ref (EntityUid Ent, float Range) closest, EntityUid pillar, EntityUid target, EntityCoordinates coords)
    {
        if (!CanTarget(pillar, target))
            return;

        var targetCoords = _transform.GetMoverCoordinates(target);
        if (!coords.TryDistance(EntityManager, targetCoords, out var distance) ||
            distance >= closest.Range)
        {
            return;
        }

        closest = (target, distance);
    }

    public override void Update(float frameTime)
    {
        if (_net.IsClient)
            return;

        var time = _timing.CurTime;
        var query = EntityQueryEnumerator<AcidPillarComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (HasComp<ActiveAcidSprayingComponent>(uid))
                comp.NextCheck = time + comp.CheckEvery;

            if (time < comp.NextCheck)
                continue;

            if (time < comp.Next)
                continue;

            comp.Next = time + comp.Cooldown;

            _marines.Clear();
            _xenos.Clear();

            var pillarCoords = _transform.GetMoverCoordinates(uid);
            _entityLookup.GetEntitiesInRange(pillarCoords, comp.Range, _marines, LookupFlags.Uncontained);
            _entityLookup.GetEntitiesInRange(pillarCoords, comp.Range, _xenos, LookupFlags.Uncontained);

            (EntityUid Ent, float Range) closest = (default, float.MaxValue);
            foreach (var marine in _marines)
            {
                TrySetIfCloserTarget(ref closest, uid, marine, pillarCoords);
            }

            foreach (var xeno in _xenos)
            {
                TrySetIfCloserTarget(ref closest, uid, xeno, pillarCoords);
            }

            if (closest.Ent == default)
                continue;

            var end = _transform.GetMoverCoordinates(closest.Ent);
            _xenoSprayAcid.CreateLine(uid, pillarCoords, end, comp.AcidSpreadDelay, comp.Range, comp.Acid, false);
            _rmcAnimation.TryFlick(uid, comp.FiringSprite, comp.IdleSprite);
        }
    }
}
