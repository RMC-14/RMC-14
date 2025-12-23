using Content.Shared._RMC14.OrbitalCannon;
using Robust.Client.GameObjects;

namespace Content.Client._RMC14.OrbitalCannon;

public sealed class OrbitalCannonVisualizerSystem : EntitySystem
{
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<OrbitalCannonComponent, ComponentStartup>(OnComponentStartup);
        SubscribeLocalEvent<OrbitalCannonComponent, AfterAutoHandleStateEvent>(OnAfterAutoHandleState);
    }

    private void OnComponentStartup(Entity<OrbitalCannonComponent> ent, ref ComponentStartup args)
    {
        UpdateSprite(ent);
    }

    private void OnAfterAutoHandleState(Entity<OrbitalCannonComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        UpdateSprite(ent);
    }

    private void UpdateSprite(Entity<OrbitalCannonComponent> ent)
    {
        if (!TryComp(ent, out SpriteComponent? sprite))
            return;

        var state = ent.Comp.Status switch
        {
            OrbitalCannonStatus.Loaded => $"{ent.Comp.BaseState}_loaded",
            OrbitalCannonStatus.Chambered => $"{ent.Comp.BaseState}_chambered",
            _ => $"{ent.Comp.BaseState}_unloaded",
        };

        _sprite.LayerSetRsiState((ent.Owner, sprite), ent.Comp.BaseLayerKey, state);
    }
}
