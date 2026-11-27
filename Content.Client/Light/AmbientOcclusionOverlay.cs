using System.Numerics;
using System.Runtime.InteropServices;
using Content.Shared.CCVar;
using Content.Shared.Maps;
using Robust.Client.Graphics;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client.Light;

/// <summary>
/// Applies ambient-occlusion to the viewport.
/// </summary>
public sealed partial class AmbientOcclusionOverlay : Overlay
{
    private static readonly ProtoId<ShaderPrototype> UnshadedShader = "unshaded";
    private static readonly ProtoId<ShaderPrototype> StencilMaskShader = "StencilMask";
    private static readonly ProtoId<ShaderPrototype> StencilEqualDrawShader = "StencilEqualDraw";
    private const float BlurMultiplier = 14f;

    [Dependency] private IClyde _clyde = default!;
    [Dependency] private IConfigurationManager _cfgManager = default!;
    [Dependency] private IEntityManager _entManager = default!;
    [Dependency] private IPrototypeManager _proto = default!;

    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowEntities;

    private IRenderTexture? _aoTarget;
    private IRenderTexture? _aoBlurBuffer;
    private List<Entity<MapGridComponent>> _cachedGrids = new();
    private readonly List<Entity<OccluderComponent, TransformComponent>> _cachedOccluders = new();
    private readonly List<Vector2> _aoVertices = new(4096);
    private readonly List<ushort> _aoIndices = new(6144);

    // Couldn't figure out a way to avoid this so if you can then please do.
    private IRenderTexture? _aoStencilTarget;

    public AmbientOcclusionOverlay()
    {
        IoCManager.InjectDependencies(this);
        ZIndex = AfterLightTargetOverlay.ContentZIndex + 1;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        /*
         * tl;dr
         * - we draw each occluder's polygon to an AO source texture.
         * - we blur this.
         * - We apply it to the viewport.
         *
         * We do this while ignoring lighting because it will wash out the actual effect.
         * In 3D ambient occlusion is more complicated due top having to calculate normals but in 2D
         * we don't have a concept of depth / corners necessarily.
         */

        var viewport = args.Viewport;
        var mapId = args.MapId;
        var worldBounds = args.WorldBounds;
        var worldHandle = args.WorldHandle;
        var color = Color.FromHex(_cfgManager.GetCVar(CCVars.AmbientOcclusionColor));
        var distance = _cfgManager.GetCVar(CCVars.AmbientOcclusionDistance);
        //var color = Color.Red;
        var target = viewport.RenderTarget;
        var lightScale = target.Size / (Vector2) viewport.Size;
        var scale = viewport.RenderScale / (Vector2.One / lightScale);
        var expandedBounds = worldBounds.Enlarged(GetBlurMargin(viewport, distance));
        var polygonExpansion = distance / EyeManager.PixelsPerMeter;
        var maps = _entManager.System<SharedMapSystem>();
        var lookups = _entManager.System<EntityLookupSystem>();
        var query = _entManager.System<OccluderSystem>();
        var xformSystem = _entManager.System<SharedTransformSystem>();
        var turfSystem = _entManager.System<TurfSystem>();
        var invMatrix = args.Viewport.GetWorldToLocalMatrix();

        if (_aoTarget?.Texture.Size != target.Size)
        {
            _aoTarget?.Dispose();
            _aoTarget = _clyde.CreateRenderTarget(target.Size, new RenderTargetFormatParameters(RenderTargetColorFormat.Rgba8Srgb), name: "ambient-occlusion-target");
        }

        if (_aoBlurBuffer?.Texture.Size != target.Size)
        {
            _aoBlurBuffer?.Dispose();
            _aoBlurBuffer = _clyde.CreateRenderTarget(target.Size, new RenderTargetFormatParameters(RenderTargetColorFormat.Rgba8Srgb), name: "ambient-occlusion-blur-target");
        }

        if (_aoStencilTarget?.Texture.Size != target.Size)
        {
            _aoStencilTarget?.Dispose();
            _aoStencilTarget = _clyde.CreateRenderTarget(target.Size, new RenderTargetFormatParameters(RenderTargetColorFormat.Rgba8Srgb), name: "ambient-occlusion-stencil-target");
        }

        // Draw the texture data to the texture.
        args.WorldHandle.RenderInRenderTarget(_aoTarget,
            () =>
            {
                worldHandle.UseShader(_proto.Index(UnshadedShader).Instance());
                worldHandle.SetTransform(Matrix3x2.Identity);
                var worldToTargetMatrix = _aoTarget.GetWorldToLocalMatrix(viewport.Eye!, scale);

                _cachedOccluders.Clear();
                query.QueryAabb(_cachedOccluders, mapId, expandedBounds);

                foreach (var entry in _cachedOccluders)
                {
                    DebugTools.Assert(entry.Comp1.Enabled);
                    var matrix = xformSystem.GetWorldMatrix(entry.Comp2);
                    var localToTargetMatrix = Matrix3x2.Multiply(matrix, worldToTargetMatrix);
                    AppendAmbientOcclusionPolygon(worldHandle, entry.Comp1.Polygon, localToTargetMatrix, polygonExpansion);
                }

                FlushAmbientOcclusionPolygons(worldHandle);
            }, Color.Transparent);

        _clyde.BlurRenderTarget(viewport, _aoTarget, _aoBlurBuffer, viewport.Eye!, BlurMultiplier);

        // Need to do stencilling after blur as it will nuke it.
        // Draw stencil for the grid so we don't draw in space.
        args.WorldHandle.RenderInRenderTarget(_aoStencilTarget,
            () =>
            {
                // Don't want lighting affecting it.
                worldHandle.UseShader(_proto.Index(UnshadedShader).Instance());

                _cachedGrids.Clear();
                maps.FindGridsIntersecting(mapId, worldBounds, ref _cachedGrids);
                foreach (var grid in _cachedGrids)
                {
                    var transform = xformSystem.GetWorldMatrix(grid.Owner);
                    var worldToTextureMatrix = Matrix3x2.Multiply(transform, invMatrix);
                    var tiles = maps.GetTilesIntersecting(grid.Owner, grid, worldBounds);
                    worldHandle.SetTransform(worldToTextureMatrix);
                    while (tiles.MoveNext(out var tileRef))
                    {
                        if (turfSystem.IsSpace(tileRef))
                            continue;

                        var bounds = lookups.GetLocalBounds(tileRef, grid.Comp.TileSize);
                        worldHandle.DrawRect(bounds, Color.White);
                    }
                }

            }, Color.Transparent);

        // Draw the stencil texture to depth buffer.
        worldHandle.UseShader(_proto.Index(StencilMaskShader).Instance());
        worldHandle.DrawTextureRect(_aoStencilTarget!.Texture, worldBounds);

        // Draw the Blurred AO texture finally.
        worldHandle.UseShader(_proto.Index(StencilEqualDrawShader).Instance());
        worldHandle.DrawTextureRect(_aoTarget!.Texture, worldBounds, color);

        args.WorldHandle.SetTransform(Matrix3x2.Identity);
        args.WorldHandle.UseShader(null);
    }

    private static float GetBlurMargin(IClydeViewport viewport, float distance)
    {
        if (viewport.Eye == null)
            return distance / EyeManager.PixelsPerMeter;

        var cameraSize = viewport.Eye.Zoom.Y * viewport.Size.Y * (1 / viewport.RenderScale.Y) / EyeManager.PixelsPerMeter;

        // Matches Clyde's BlurRenderTarget radius calculation closely enough to include off-screen AO contributors.
        return distance / EyeManager.PixelsPerMeter + BlurMultiplier / cameraSize;
    }

    private void AppendAmbientOcclusionPolygon(
        DrawingHandleWorld worldHandle,
        ReadOnlySpan<Vector2> polygon,
        Matrix3x2 localToTargetMatrix,
        float expansion)
    {
        if (polygon.Length < 3)
            return;

        // Keep indices representable as ushort for DrawingHandleBase.DrawPrimitives().
        if (_aoVertices.Count + polygon.Length > ushort.MaxValue)
            FlushAmbientOcclusionPolygons(worldHandle);

        var indexBase = (ushort) _aoVertices.Count;
        var center = Vector2.Zero;

        for (var i = 0; i < polygon.Length; i++)
        {
            center += polygon[i];
        }

        center /= polygon.Length;

        for (var i = 0; i < polygon.Length; i++)
        {
            var vertex = polygon[i];

            if (expansion > 0f)
            {
                var offset = vertex - center;
                if (offset.LengthSquared() > 0f)
                    vertex += Vector2.Normalize(offset) * expansion;
            }

            _aoVertices.Add(Vector2.Transform(vertex, localToTargetMatrix));
        }

        for (var i = 1; i < polygon.Length - 1; i++)
        {
            _aoIndices.Add(indexBase);
            _aoIndices.Add((ushort) (indexBase + i));
            _aoIndices.Add((ushort) (indexBase + i + 1));
        }
    }

    private void FlushAmbientOcclusionPolygons(DrawingHandleWorld worldHandle)
    {
        if (_aoVertices.Count == 0)
            return;

        worldHandle.DrawPrimitives(
            DrawPrimitiveTopology.TriangleList,
            CollectionsMarshal.AsSpan(_aoIndices),
            CollectionsMarshal.AsSpan(_aoVertices),
            Color.White);

        _aoVertices.Clear();
        _aoIndices.Clear();
    }
}
