using Content.Shared._RMC14.Sprite;
using Content.Shared._RMC14.Stun;
using Content.Shared._RMC14.Xenonids;

namespace Content.Shared._RMC14.TallGrass;

public sealed class TallGrassSystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedRMCSpriteSystem _rmcSprite = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private readonly HashSet<Entity<TallGrassComponent>> _nearbyGrass = new();

    private static readonly HashSet<RMCSizes> HideableSizes = new()
    {
        RMCSizes.Small,
        RMCSizes.VerySmallXeno,
        RMCSizes.SmallXeno,
    };

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HiddenInGrassComponent, GetDrawDepthEvent>(OnHiddenGetDrawDepth);
        SubscribeLocalEvent<HiddenInGrassComponent, ComponentStartup>(OnHiddenStartup);
    }

    private void OnHiddenGetDrawDepth(Entity<HiddenInGrassComponent> ent, ref GetDrawDepthEvent args)
    {
        args.DrawDepth = Content.Shared.DrawDepth.DrawDepth.SmallMobs;
    }

    private void OnHiddenStartup(Entity<HiddenInGrassComponent> ent, ref ComponentStartup args)
    {
        _rmcSprite.UpdateDrawDepth(ent);
    }

    private bool IsInGrass(EntityUid uid)
    {
        var coords = _transform.GetMoverCoordinates(uid);
        _nearbyGrass.Clear();
        _lookup.GetEntitiesInRange(coords, 0.5f, _nearbyGrass);
        return _nearbyGrass.Count > 0;
    }

    private bool CanHideInGrass(EntityUid uid)
    {
        return TryComp(uid, out RMCSizeComponent? size) && HideableSizes.Contains(size.Size);
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<XenoComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out _, out var xform))
        {
            if (xform.MapUid == null)
                continue;

            if (!CanHideInGrass(uid))
                continue;

            var inGrass = IsInGrass(uid);

            if (inGrass && !HasComp<HiddenInGrassComponent>(uid))
            {
                EnsureComp<HiddenInGrassComponent>(uid);
                _rmcSprite.UpdateDrawDepth(uid);
            }
            else if (!inGrass && HasComp<HiddenInGrassComponent>(uid))
            {
                RemComp<HiddenInGrassComponent>(uid);
                _rmcSprite.UpdateDrawDepth(uid);
            }
        }
    }
}
