using Content.Shared._RMC14.TallGrass;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Prototypes;

namespace Content.Client.Movement.Systems;

public sealed class FloorOcclusionSystem : SharedFloorOcclusionSystem
{
    private static readonly ProtoId<ShaderPrototype> HorizontalCut = "HorizontalCut";

    [Dependency] private readonly EntityLookupSystem _clientLookup = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SharedTransformSystem _clientTransform = default!;

    private EntityQuery<SpriteComponent> _spriteQuery;
    private readonly HashSet<Entity<FloorOccluderComponent>> _clientOccluders = new();
    private readonly HashSet<EntityUid> _shaderApplied = new();

    public override void Initialize()
    {
        base.Initialize();

        _spriteQuery = GetEntityQuery<SpriteComponent>();

        SubscribeLocalEvent<FloorOcclusionComponent, ComponentShutdown>(OnOcclusionShutdown);
    }

    private void OnOcclusionShutdown(Entity<FloorOcclusionComponent> ent, ref ComponentShutdown args)
    {
        SetShader(ent.Owner, false);
    }

#pragma warning disable RA0028
    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<FloorOcclusionComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out _, out var xform))
        {
            if (xform.MapUid == null)
                continue;

            if (HasComp<HiddenInGrassComponent>(uid))
            {
                SetShader(uid, false);
                continue;
            }

            var coords = _clientTransform.GetMoverCoordinates(uid);
            _clientOccluders.Clear();
            _clientLookup.GetEntitiesInRange(coords, 0.5f, _clientOccluders);

            SetShader(uid, _clientOccluders.Count > 0);
        }
    }

    private void SetShader(Entity<SpriteComponent?> sprite, bool enabled)
    {
        if (!_spriteQuery.Resolve(sprite.Owner, ref sprite.Comp, false))
            return;

        var shader = _proto.Index(HorizontalCut).Instance();

        if (enabled)
        {
            // Don't overwrite another system's PostShader.
            if (sprite.Comp.PostShader is not null && sprite.Comp.PostShader != shader)
                return;

            sprite.Comp.PostShader = shader;
            _shaderApplied.Add(sprite.Owner);
        }
        else
        {
            if (!_shaderApplied.Remove(sprite.Owner))
                return;

            if (sprite.Comp.PostShader == shader)
                sprite.Comp.PostShader = null;
        }
    }
}
