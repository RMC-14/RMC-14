using Content.Shared._RMC14.Vehicle;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;

namespace Content.Client._RMC14.Vehicle;

public sealed class VehicleHardpointVisualizerSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<VehicleHardpointVisualsComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<VehicleHardpointVisualsComponent, AfterAutoHandleStateEvent>(OnHandleState);
    }

    private void OnStartup(Entity<VehicleHardpointVisualsComponent> ent, ref ComponentStartup args)
    {
        ApplyLayers(ent, ent.Comp);
    }

    private void OnHandleState(Entity<VehicleHardpointVisualsComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        ApplyLayers(ent, ent.Comp);
    }

    private void ApplyLayers(EntityUid uid, VehicleHardpointVisualsComponent component)
    {
        if (!TryComp(uid, out SpriteComponent? sprite))
            return;

        foreach (var entry in component.Layers)
        {
            UpdateLayer(sprite, entry.Layer, entry.State);
        }
    }

    private void UpdateLayer(SpriteComponent sprite, string layerMap, string state)
    {
        if (!sprite.LayerMapTryGet(layerMap, out var layer))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(state))
        {
            sprite.LayerSetVisible(layer, false);
            return;
        }

        sprite.LayerSetState(layer, state);
        sprite.LayerSetVisible(layer, true);
    }
}
