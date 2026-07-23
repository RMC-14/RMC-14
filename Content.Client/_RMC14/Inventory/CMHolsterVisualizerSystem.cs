using Content.Client.Clothing;
using Content.Client.Items.Systems;
using Content.Shared._RMC14.Inventory;
using Content.Shared.Clothing;
using Content.Shared.Hands;
using Robust.Client.GameObjects;

namespace Content.Client._RMC14.Inventory;

/// <summary>
/// Sets the carried and equipped visuals of holsters.
/// </summary>
public sealed class CMHolsterVisualizerSystem : VisualizerSystem<CMHolsterComponent>
{
    [Dependency] private readonly ItemSystem _item = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CMHolsterComponent, GetEquipmentVisualsEvent>(OnGetEquipmentVisuals, after: [typeof(ClientClothingSystem)]);
        SubscribeLocalEvent<CMHolsterComponent, GetInhandVisualsEvent>(OnGetInhandVisuals, after: [typeof(ItemSystem)]);
    }

    protected override void OnAppearanceChange(EntityUid uid,
        CMHolsterComponent component,
        ref AppearanceChangeEvent args)
    {
        if (args.Sprite is not { } sprite)
            return;

        AppearanceSystem.TryGetData(uid, CMHolsterLayers.Fill, out CMHolsterVisuals visual, args.Component);

        if (sprite.LayerMapTryGet(CMHolsterLayers.Fill, out var layer))
        {
            // TODO: implement per-gun underlay here
            // sprite.LayerSetState(layer, $"{<gun_state_here>}");
            sprite.LayerSetVisible(layer, visual == CMHolsterVisuals.Full);
        }

        if (component.FullEquippedState != null || component.FullInhandPrefix != null)
            _item.VisualsChanged(uid);
    }

    private void OnGetEquipmentVisuals(Entity<CMHolsterComponent> ent, ref GetEquipmentVisualsEvent args)
    {
        if (ent.Comp.FullEquippedState == null ||
            args.Layers.Count == 0 ||
            !TryComp<AppearanceComponent>(ent, out var appearance) ||
            !AppearanceSystem.TryGetData(ent, CMHolsterLayers.Fill, out CMHolsterVisuals visual, appearance) ||
            visual != CMHolsterVisuals.Full)
        {
            return;
        }

        var (key, layer) = args.Layers[0];
        args.Layers[0] = (key, WithState(layer, ent.Comp.FullEquippedState));
    }

    private void OnGetInhandVisuals(Entity<CMHolsterComponent> ent, ref GetInhandVisualsEvent args)
    {
        if (ent.Comp.FullInhandPrefix == null ||
            args.Layers.Count == 0 ||
            !TryComp<AppearanceComponent>(ent, out var appearance) ||
            !AppearanceSystem.TryGetData(ent, CMHolsterLayers.Fill, out CMHolsterVisuals visual, appearance) ||
            visual != CMHolsterVisuals.Full)
        {
            return;
        }

        var state = $"{ent.Comp.FullInhandPrefix}-inhand-{args.Location.ToString().ToLowerInvariant()}";
        var (key, layer) = args.Layers[0];
        args.Layers[0] = (key, WithState(layer, state));
    }

    private static PrototypeLayerData WithState(PrototypeLayerData layer, string state)
    {
        return new PrototypeLayerData
        {
            Shader = layer.Shader,
            TexturePath = layer.TexturePath,
            RsiPath = layer.RsiPath,
            State = state,
            Scale = layer.Scale,
            Rotation = layer.Rotation,
            Offset = layer.Offset,
            Visible = layer.Visible,
            Color = layer.Color,
            MapKeys = layer.MapKeys is null ? null : new(layer.MapKeys),
            RenderingStrategy = layer.RenderingStrategy,
            CopyToShaderParameters = layer.CopyToShaderParameters,
            Cycle = layer.Cycle,
        };
    }
}
