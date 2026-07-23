using System.Numerics;
using Content.Shared._RMC14.Furniture;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using DrawDepth = Content.Shared.DrawDepth.DrawDepth;

namespace Content.Client._RMC14.Furniture;

public sealed class RMCChairStackVisualizerSystem : EntitySystem
{
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    private const string StackLayerPrefix = "rmc_chair_stack_";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RMCChairStackableComponent, AppearanceChangeEvent>(OnAppearanceChange);
    }

    private void OnAppearanceChange(Entity<RMCChairStackableComponent> ent, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (!_appearance.TryGetData<int>(ent, RMCChairStackVisuals.StackSize, out var stackSize))
            stackSize = 0;

        UpdateStackLayers(ent, args.Sprite, stackSize, ent.Comp.MaxStableStack);

        // Raise draw depth above mobs when stacked, reset when unstacked
        Entity<SpriteComponent?> spriteEnt = (ent, args.Sprite);
        if (stackSize > 0)
            _sprite.SetDrawDepth(spriteEnt, (int) DrawDepth.OverMobs);
        else
            _sprite.SetDrawDepth(spriteEnt, (int) DrawDepth.Objects);
    }

    private void UpdateStackLayers(EntityUid uid, SpriteComponent sprite, int stackSize, int maxStableStack)
    {
        Entity<SpriteComponent?> spriteEnt = (uid, sprite);

        var toRemove = new List<(string Key, int Index)>();
        for (var i = 0; i < 50; i++)
        {
            var key = StackLayerPrefix + i;
            if (_sprite.LayerMapTryGet(spriteEnt, key, out var index, false))
                toRemove.Add((key, index));
            else
                break;
        }

        toRemove.Sort((a, b) => b.Index.CompareTo(a.Index));
        foreach (var (key, index) in toRemove)
        {
            _sprite.LayerMapRemove(spriteEnt, key);
            _sprite.RemoveLayer(spriteEnt, index);
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
        float deltaX;
        float deltaY;

        var dir = Transform(uid).LocalRotation.GetCardinalDir();
        switch (dir)
        {
            case Direction.South:
                deltaX = 0;
                deltaY = 2 * pxToWorld;
                break;
            case Direction.East:
                deltaX = 1 * pxToWorld;
                deltaY = 3 * pxToWorld;
                break;
            case Direction.North:
                deltaX = 0;
                deltaY = 2 * pxToWorld;
                break;
            case Direction.West:
                deltaX = -1 * pxToWorld;
                deltaY = 3 * pxToWorld;
                break;
            default:
                deltaX = 0;
                deltaY = 2 * pxToWorld;
                break;
        }

        for (var i = 0; i < stackSize; i++)
        {
            var level = i + 1; // level 1 = first stacked chair above base
            var offsetX = deltaX * level;
            var offsetY = deltaY * level;

            // if(stacked_size > 8) I.pixel_x += pick(list(-1, 1))
            if (stackSize > maxStableStack)
                offsetX += (i % 2 == 0 ? -1 : 1) * pxToWorld;

            var layerData = new PrototypeLayerData
            {
                RsiPath = rsi.ToString(),
                State = state,
                Offset = new Vector2(offsetX, offsetY),
                Visible = true,
            };

            var key = StackLayerPrefix + i;
            var layerIndex = _sprite.AddLayer(spriteEnt, layerData, null);
            _sprite.LayerMapSet(spriteEnt, key, layerIndex);
        }
    }
}
