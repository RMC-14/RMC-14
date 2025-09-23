using System.Numerics;
using Content.Shared._RMC14.Xenonids.Eye;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client._RMC14.Xenonids.Eye;

public sealed class QueenEyeOverlay : Overlay
{
    [Dependency] private readonly IClyde _clyde = default!;
    [Dependency] private readonly IEntityManager _entities = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    private readonly HashSet<Vector2i> _visibleTiles = new();

    private IRenderTexture? _staticTexture;
    private IRenderTexture? _stencilTexture;

    private readonly float _updateRate = 1f / 30f;
    private float _accumulator;

    public QueenEyeOverlay()
    {
        IoCManager.InjectDependencies(this);
        ZIndex = 2;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (_stencilTexture?.Texture.Size != args.Viewport.Size)
        {
            _staticTexture?.Dispose();
            _stencilTexture?.Dispose();
            _stencilTexture = _clyde.CreateRenderTarget(args.Viewport.Size, new RenderTargetFormatParameters(RenderTargetColorFormat.Rgba8Srgb), name: "queen-eye-stencil");
            _staticTexture = _clyde.CreateRenderTarget(args.Viewport.Size,
                new RenderTargetFormatParameters(RenderTargetColorFormat.Rgba8Srgb),
                name: "queen-eye-static");
        }

        var worldHandle = args.WorldHandle;

        var worldBounds = args.WorldBounds;

        var playerEnt = _player.LocalEntity;
        _entities.TryGetComponent(playerEnt, out TransformComponent? playerXform);
        var gridUid = playerXform?.GridUid ?? EntityUid.Invalid;
        _entities.TryGetComponent(gridUid, out MapGridComponent? grid);
        _entities.TryGetComponent(gridUid, out BroadphaseComponent? broadphase);

        var invMatrix = args.Viewport.GetWorldToLocalMatrix();
        _accumulator -= (float) _timing.FrameTime.TotalSeconds;

        if (grid != null && broadphase != null)
        {
            var lookups = _entities.System<EntityLookupSystem>();
            var xforms = _entities.System<SharedTransformSystem>();

            if (_accumulator <= 0f)
            {
                _accumulator = MathF.Max(0f, _accumulator + _updateRate);
                _visibleTiles.Clear();
                _entities.System<QueenEyeSystem>().GetView((gridUid, broadphase, grid), worldBounds, _visibleTiles);
            }

            var gridMatrix = xforms.GetWorldMatrix(gridUid);
            var matty =  Matrix3x2.Multiply(gridMatrix, invMatrix);

            // Draw visible tiles to stencil
            worldHandle.RenderInRenderTarget(_stencilTexture!,
                () =>
                {
                    worldHandle.SetTransform(matty);

                    foreach (var tile in _visibleTiles)
                    {
                        var aabb = lookups.GetLocalBounds(tile, grid.TileSize);
                        worldHandle.DrawRect(aabb, Color.White);
                    }
                },
                Color.Transparent);

            // Once this is gucci optimise rendering.
            worldHandle.RenderInRenderTarget(_staticTexture!,
            () =>
            {
                worldHandle.SetTransform(invMatrix);
                // var shader = _proto.Index<ShaderPrototype>("CameraStatic").Instance();
                // worldHandle.UseShader(shader);
                worldHandle.DrawRect(worldBounds, Color.Black);
            },
            Color.Black);
        }
        // Not on a grid
        else
        {
            worldHandle.RenderInRenderTarget(_stencilTexture!,
                () =>
                {
                },
                Color.Transparent);

            worldHandle.RenderInRenderTarget(_staticTexture!,
                () =>
                {
                    worldHandle.SetTransform(Matrix3x2.Identity);
                    worldHandle.DrawRect(worldBounds, Color.Black);
                },
                Color.Black);
        }

        // Use the lighting as a mask
        worldHandle.UseShader(_proto.Index<ShaderPrototype>("StencilMask").Instance());
        worldHandle.DrawTextureRect(_stencilTexture!.Texture, worldBounds);

        // Draw the static
        worldHandle.UseShader(_proto.Index<ShaderPrototype>("StencilDraw").Instance());
        worldHandle.DrawTextureRect(_staticTexture!.Texture, worldBounds);

        worldHandle.SetTransform(Matrix3x2.Identity);
        worldHandle.UseShader(null);
    }
}
