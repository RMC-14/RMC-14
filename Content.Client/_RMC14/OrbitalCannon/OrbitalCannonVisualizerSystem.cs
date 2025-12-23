using Content.Shared._RMC14.OrbitalCannon;
using Robust.Client.GameObjects;

namespace Content.Client._RMC14.OrbitalCannon;

public sealed class OrbitalCannonVisualizerSystem : EntitySystem
{
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<OrbitalCannonComponent, AfterAutoHandleStateEvent>(OnAfterAutoHandleState);
    }

    private void OnAfterAutoHandleState(Entity<OrbitalCannonComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (!TryComp(ent, out SpriteComponent? sprite))
            return;

        var state = ent.Comp.Status switch
        {
            OrbitalCannonStatus.Loaded => ent.Comp.LoadedState,
            OrbitalCannonStatus.Chambered => ent.Comp.ChamberedState,
            _ => ent.Comp.UnloadedState,
        };

        _sprite.LayerSetSprite((ent.Owner, sprite), ent.Comp.BaseLayerKey, state);
    }
}
