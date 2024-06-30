using Content.Shared.Buckle.Components;
using Robust.Shared.Physics.Events;

namespace Content.Shared._RMC14.Buckle;

public sealed class CMBuckleSystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;

    private readonly HashSet<EntityUid> _intersecting = new();

    public override void Initialize()
    {
        SubscribeLocalEvent<BuckleClimbableComponent, StrappedEvent>(OnBuckleClimbableStrapped);

        SubscribeLocalEvent<ActiveBuckleClimbingComponent, PreventCollideEvent>(OnBuckleClimbablePreventCollide);
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
