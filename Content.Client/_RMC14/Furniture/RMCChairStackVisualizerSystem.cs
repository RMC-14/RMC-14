using System.Numerics;
using Content.Shared._RMC14.Furniture;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using DrawDepth = Content.Shared.DrawDepth.DrawDepth;

namespace Content.Client._RMC14.Furniture;

public sealed class RMCChairStackVisualizerSystem : VisualizerSystem<RMCChairStackableComponent>
{
    private const string StackLayerPrefix = "rmc_chair_stack_";

    protected override void OnAppearanceChange(EntityUid uid, RMCChairStackableComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (!_appearance.TryGetData<int>(ent, RMCChairStackVisuals.StackSize, out var stackSize))
            stackSize = 0;

        UpdateStackLayers(ent, args.Sprite, stackSize, ent.Comp.MaxStableStack);

        // Raise draw depth above mobs when stacked, reset when unstacked
        Entity<SpriteComponent?> spriteEnt = (ent, args.Sprite);
        if (stackSize > 0)
            _sprite.SetDrawDepth(spriteEnt, (int)DrawDepth.OverMobs);
        else
            _sprite.SetDrawDepth(spriteEnt, (int)DrawDepth.Objects);
    }

    private void UpdateStackLayers(EntityUid uid, SpriteComponent sprite, int stackSize, int maxStableStack)
    {
        Entity<SpriteComponent?> spriteEnt = (uid, sprite);

        var oldChairIdx = 0;
        var oldChairKey = StackLayerPrefix + oldChairIdx;
        while (SpriteSystem.LayerMapTryGet(spriteEnt, oldChairKey, out var index, false))
        {
            SpriteSystem.LayerMapRemove(spriteEnt, oldChairKey);
            SpriteSystem.RemoveLayer(spriteEnt, index);

            oldChairIdx++;
            oldChairKey = StackLayerPrefix + oldChairIdx;
        }

        if (stackSize <= 0)
            return;

        // Get the RSI and state from the first (unfolded) layer for the overlay sprite
        var rsi = _sprite.LayerGetEffectiveRsi(spriteEnt, 0)?.Path;
        if (rsi == null)
            return;

        var state = _sprite.LayerGetRsiState(spriteEnt, 0).ToString();
        if (string.IsNullOrWhiteSpace(state))
            return;

        const float pxToWorld = 1f / EyeManager.PixelsPerMeter;

        var dir = Transform(spriteEnt).LocalRotation.GetCardinalDir();
        var delta = dir switch
        {
            Direction.East => new Vector2(1 * pxToWorld, 3 * pxToWorld),
            Direction.West => new Vector2(-1 * pxToWorld, 3 * pxToWorld),
            // North and south both have the same offset.
            _ => new Vector2(0, 2 * pxToWorld)
        };

        for (var i = 0; i < stackSize; i++)
        {
            var level = i + 1; // level 1 = first stacked chair above base
            var offset = delta * level;

            // if(stacked_size > 8) I.pixel_x += pick(list(-1, 1))
            if (stackSize > maxStableStack)
                offset.X += (i % 2 == 0 ? -1 : 1) * pxToWorld;

            var layerData = new PrototypeLayerData
            {
                RsiPath = rsi.ToString(),
                State = state,
                Offset = offset,
                Visible = true,
            };

            var key = StackLayerPrefix + i;
            var layerIndex = _sprite.AddLayer(spriteEnt, layerData, null);
            _sprite.LayerMapSet(spriteEnt, key, layerIndex);
        }
    }
}
