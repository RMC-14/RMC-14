using Content.Client.Smoking;
using Content.Shared._RMC14.GhostAppearance;
using Content.Shared._RMC14.Xenonids;
using Content.Shared._RMC14.Xenonids.Damage;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Prototypes;

namespace Content.Client._RMC14.GhostAppearance;

public sealed class DeadGhostVisualsSystem : SharedDeadGhostVisualsSystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;

    private EntityQuery<AppearanceComponent> _appearanceQuery;

    private readonly float _opacity = 0.5f;

    public override void Initialize()
    {
        base.Initialize();

        _appearanceQuery = GetEntityQuery<AppearanceComponent>();

        SubscribeLocalEvent<RMCGhostAppearanceComponent, AfterAutoHandleStateEvent>(OnHandleState);
    }

    private void OnHandleState(Entity<RMCGhostAppearanceComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        CopyComp<SpriteComponent>(ent);
        CopyComp<GenericVisualizerComponent>(ent);
        CopyComp<BurnStateVisualsComponent>(ent);

        // reload appearance to hopefully prevent any invisible layers
        if (_appearanceQuery.TryComp(ent, out var appearance))
            _appearance.QueueUpdate(ent, appearance);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var entities = EntityQueryEnumerator<RMCGhostAppearanceComponent, SpriteComponent>();
        while (entities.MoveNext(out var ent, out var ghost, out var sprite))
        {
            sprite.PostShader = _prototypes.Index<ShaderPrototype>("RMCInvisible").InstanceUnique();
            sprite.PostShader.SetParameter("visibility", _opacity);

            if (GetEntity(ghost.SourceEntity) is { } source && !ghost.Updated)
            {
                _sprite.CopySprite(source, ent);

                if (HasComp<XenoComponent>(source)) // update xeno visuals
                {
                    if (sprite is { BaseRSI: { } rsi } && _sprite.LayerMapTryGet(ent, XenoVisualLayers.Base, out var layer, false))
                    {
                        if (rsi.TryGetState("alive", out _))
                            _sprite.LayerSetRsiState(ent, layer, "alive");

                        if (_sprite.LayerMapTryGet(ent, RMCDamageVisualLayers.Base, out var damageLayer, false))
                            _sprite.LayerSetVisible(ent, damageLayer, false); // set damage visuals invisible
                    }
                }

                ghost.Updated = true;
            }
        }
    }
}
