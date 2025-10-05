using Content.Shared._RMC14.Xenonids.Egg;
using Content.Shared._RMC14.Xenonids.Fortify;
using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared._RMC14.Xenonids.Rest;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Standing;
using Content.Shared.StatusEffect;
using Content.Shared.Stunnable;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Xenonids.Collision;

public sealed class XenoCollisionSystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly SharedXenoHiveSystem _hive = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly StandingStateSystem _standingState = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly XenoRestSystem _xenoRest = default!;

    private EntityQuery<MobCollisionComponent> _mobCollisionQuery;
    private EntityQuery<StunFriendlyXenoOnStepComponent> _stunFriendlyXenoOnStepQuery;
    private EntityQuery<XenoFortifyComponent> _xenoFortifyQuery;

    private readonly HashSet<EntityUid> _contacts = new();

    public override void Initialize()
    {
        _mobCollisionQuery = GetEntityQuery<MobCollisionComponent>();
        _stunFriendlyXenoOnStepQuery = GetEntityQuery<StunFriendlyXenoOnStepComponent>();
        _xenoFortifyQuery = GetEntityQuery<XenoFortifyComponent>();

        SubscribeLocalEvent<XenoComponent, AttemptMobTargetCollideEvent>(OnXenoAttemptMobTargetCollide);

        SubscribeLocalEvent<StunFriendlyXenoOnStepComponent, MobStateChangedEvent>(OnStunUpdated);
        SubscribeLocalEvent<StunFriendlyXenoOnStepComponent, XenoRestEvent>(OnStunUpdated);
        SubscribeLocalEvent<StunFriendlyXenoOnStepComponent, StunnedEvent>(OnStunUpdated);
        SubscribeLocalEvent<StunFriendlyXenoOnStepComponent, StatusEffectEndedEvent>(OnStunUpdated);
        SubscribeLocalEvent<StunFriendlyXenoOnStepComponent, XenoOvipositorChangedEvent>(OnStunUpdated);
        SubscribeLocalEvent<StunFriendlyXenoOnStepComponent, PreventCollideEvent>(OnPreventCollide);
    }

    private void OnXenoAttemptMobTargetCollide(Entity<XenoComponent> ent, ref AttemptMobTargetCollideEvent args)
    {
        if (args.Cancelled)
            return;

        if (_stunFriendlyXenoOnStepQuery.HasComp(args.Entity))
            args.Cancelled = true;
    }

    private void OnStunUpdated<T>(Entity<StunFriendlyXenoOnStepComponent> ent, ref T args)
    {
        ent.Comp.Enabled = _mobState.IsAlive(ent) &&
                           !HasComp<XenoRestingComponent>(ent) &&
                           !_statusEffects.HasStatusEffect(ent, ent.Comp.DisableStatus) &&
                           CompOrNull<XenoAttachedOvipositorComponent>(ent) is not { Running: true };
        Dirty(ent);
    }

    private void OnPreventCollide(Entity<StunFriendlyXenoOnStepComponent> ent, ref PreventCollideEvent args)
    {
        if (_xenoFortifyQuery.TryComp(args.OtherEntity, out var fortify) &&
            fortify.Fortified)
        {
            args.Cancelled = true;
        }
    }

    public override void Update(float frameTime)
    {
        var time = _timing.CurTime;
        var query = EntityQueryEnumerator<StunFriendlyXenoOnStepComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var comp, out var xform))
        {
            if (!comp.Enabled)
                continue;

            _contacts.Clear();
            _physics.GetContactingEntities(uid, _contacts);

            foreach (var other in _contacts)
            {
                if (_mobState.IsDead(other) ||
                    _xenoRest.IsResting(other) ||
                    _standingState.IsDown(other) ||
                    !_mobCollisionQuery.HasComp(other))
                {
                    continue;
                }

                var otherTransform = Transform(other);
                var ourAabb = _entityLookup.GetAABBNoContainer(uid, xform.LocalPosition, xform.LocalRotation);
                var otherAabb = _entityLookup.GetAABBNoContainer(other, otherTransform.LocalPosition, otherTransform.LocalRotation);
                if (!ourAabb.Intersects(otherAabb))
                    continue;

                var intersect = Box2.Area(otherAabb.Intersect(ourAabb));
                var ratio = Math.Max(intersect / Box2.Area(otherAabb), intersect / Box2.Area(ourAabb));
                if (ratio < comp.Ratio)
                    continue;

                if (!_hive.FromSameHive(uid, other))
                    continue;

                var recently = EnsureComp<RecentlyStunnedByFriendlyXenoComponent>(other);
                if (time < recently.At + comp.Cooldown)
                    continue;

                recently.At = time;
                Dirty(other, recently);

                _stun.TryParalyze(other, comp.Duration, true, force: true);
            }
        }
    }
}
