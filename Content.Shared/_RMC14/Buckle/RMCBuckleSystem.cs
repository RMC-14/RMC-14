using System.Numerics;
using Content.Shared._RMC14.CrashLand;
using Content.Shared.Buckle;
using Content.Shared.Buckle.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Shuttles.Components;
using Content.Shared.Whitelist;
using Robust.Shared.Physics.Events;

namespace Content.Shared._RMC14.Buckle;

public sealed class RMCBuckleSystem : EntitySystem
{
    [Dependency] private readonly SharedBuckleSystem _buckle = default!;
    [Dependency] private readonly SharedCrashLandSystem _crashLand = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly EntityWhitelistSystem _entityWhitelist = default!;

    private readonly HashSet<EntityUid> _intersecting = new();

    public override void Initialize()
    {
        SubscribeLocalEvent<BuckleClimbableComponent, StrappedEvent>(OnBuckleClimbableStrapped);
        SubscribeLocalEvent<ActiveBuckleClimbingComponent, PreventCollideEvent>(OnBuckleClimbablePreventCollide);
        SubscribeLocalEvent<BuckleWhitelistComponent, BuckleAttemptEvent>(OnBuckleWhitelistAttempt);
        SubscribeLocalEvent<BuckleComponent, AttemptMobTargetCollideEvent>(OnBuckleAttemptMobTargetCollide);
        SubscribeLocalEvent<StrapComponent, EntParentChangedMessage>(OnBuckleParentChanged);
    }

    private void OnBuckleClimbableStrapped(Entity<BuckleClimbableComponent> ent, ref StrappedEvent args)
    {
        var active = EnsureComp<ActiveBuckleClimbingComponent>(args.Buckle);
        active.Strap = ent;
        Dirty(args.Buckle, active);
    }

    private void OnBuckleClimbablePreventCollide(Entity<ActiveBuckleClimbingComponent> ent, ref PreventCollideEvent args)
    {
        if (args.Cancelled)
            return;

        if (ent.Comp.Strap == args.OtherEntity)
            args.Cancelled = true;
    }

    private void OnBuckleWhitelistAttempt(Entity<BuckleWhitelistComponent> ent, ref BuckleAttemptEvent args)
    {
        if (!_entityWhitelist.IsWhitelistPassOrNull(ent.Comp.Whitelist, args.Strap))
            args.Cancelled = true;
    }

    private void OnBuckleAttemptMobTargetCollide(Entity<BuckleComponent> ent, ref AttemptMobTargetCollideEvent args)
    {
        if (args.Cancelled)
            return;

        if (ent.Comp.Buckled)
            args.Cancelled = true;
    }

    private void OnBuckleParentChanged(Entity<StrapComponent> ent, ref EntParentChangedMessage args)
    {
        if (!HasComp<FTLMapComponent>(args.Transform.ParentUid) || args.OldParent == null)
            return;

        foreach (var entity in ent.Comp.BuckledEntities)
        {
            _buckle.TryUnbuckle(entity, entity, false);
            var ev = new AttemptCrashLandEvent(entity);
            RaiseLocalEvent(args.OldParent.Value, ref ev);

            if (!ev.Cancelled)
                _crashLand.TryCrashLand(entity, true);
        }
    }

    public Vector2 GetOffset(Entity<RMCBuckleOffsetComponent?> offset)
    {
        if (!Resolve(offset, ref offset.Comp, false))
            return Vector2.Zero;

        return offset.Comp.Offset;
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<ActiveBuckleClimbingComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.Strap is not { } strap)
            {
                RemCompDeferred<ActiveBuckleClimbingComponent>(uid);
                continue;
            }

            _intersecting.Clear();
            _entityLookup.GetEntitiesIntersecting(uid, _intersecting);

            if (!_intersecting.Contains(strap))
                RemCompDeferred<ActiveBuckleClimbingComponent>(uid);
        }
    }
}
