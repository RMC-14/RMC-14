using Content.Shared._RMC14.Xenonids;
using Content.Shared._RMC14.Xenonids.Inhands;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;

namespace Content.Client._RMC14.Xenonids.Inhands;

public sealed class XenoInhandsVisualsSystem : VisualizerSystem<XenoInhandsComponent>
{
    [Dependency] private readonly SpriteSystem _sprite = default!;

    private static readonly XenoInhandVisualLayers[] HandLayers = new[] { XenoInhandVisualLayers.Left, XenoInhandVisualLayers.Right };

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<XenoInhandsComponent, AfterAutoHandleStateEvent>(OnStateChanged);
        SubscribeLocalEvent<XenoInhandsComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnStateChanged(Entity<XenoInhandsComponent> entity, ref AfterAutoHandleStateEvent args)
    {
        // Layer states will become invalid, so we disable the layers here.
        // The layers will become re-enabled when the appearance data is updated.
        DisableHandLayers(entity);
    }
    private void OnShutdown(Entity<XenoInhandsComponent> entity, ref ComponentShutdown shutdown)
    {
        DisableHandLayers(entity);
    }

    private void DisableHandLayers(EntityUid entity)
    {
        TryComp<SpriteComponent>(entity, out var spriteComp);

        if (spriteComp == null)
        {
            return;
        }

        var sprite = (entity, spriteComp);

        foreach (var layerDef in HandLayers)
        {
            if (!_sprite.LayerMapTryGet(sprite, layerDef, out var layer, false))
                continue;

            _sprite.LayerSetRsiState(sprite, layer, null);
            _sprite.LayerSetVisible(sprite, layer, false);
        }
    }

    protected override void OnAppearanceChange(EntityUid uid, XenoInhandsComponent component, ref AppearanceChangeEvent args)
    {
        var sprite = args.Sprite;

        if (sprite == null)
            return;

        if (!AppearanceSystem.TryGetData(uid, XenoInhandVisuals.RightHand, out string right))
            return;

        if (!AppearanceSystem.TryGetData(uid, XenoInhandVisuals.LeftHand, out string left))
            return;

        bool downed = false;
        bool resting = false;
        bool ovi = false;

        AppearanceSystem.TryGetData(uid, RMCXenoStateVisuals.Downed, out downed);
        AppearanceSystem.TryGetData(uid, RMCXenoStateVisuals.Resting, out resting);
        AppearanceSystem.TryGetData(uid, RMCXenoStateVisuals.Ovipositor, out ovi);

        foreach (var layerDef in HandLayers)
        {
            string name = layerDef switch
            {
                XenoInhandVisualLayers.Left => left,
                XenoInhandVisualLayers.Right => right,
                _ => string.Empty,
            };

            if (!sprite.LayerMapTryGet(layerDef, out var layer))
                continue;

            if (name == string.Empty)
            {
                sprite.LayerSetVisible(layer, false);
            }
            else
            {
                sprite.LayerSetVisible(layer, true);

                string stateString = $"{component.Prefix}_{name}_{layerDef.ToString().ToLower()}";


                if (ovi)
                    stateString += "_" + component.Ovi;
                else if (downed)
                    stateString += "_" + component.Downed;
                else if (resting)
                    stateString += "_" + component.Resting;

                RSI? rsi = sprite.LayerGetActualRSI(layerDef);

                if (rsi == null)
                    continue;

                rsi.TryGetState(stateString, out var state);

                if (state != null)
                {
                    sprite.LayerSetState(layer, stateString);
                }
                else
                    sprite.LayerSetVisible(layer, false);
            }
        }
    }
}
