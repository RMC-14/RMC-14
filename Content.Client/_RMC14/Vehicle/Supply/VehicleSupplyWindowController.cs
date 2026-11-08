using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Content.Shared._RMC14.Vehicle;
using Content.Shared._RMC14.Vehicle.Supply;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Utility;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.GameObjects;
using Robust.Shared.Graphics.RSI;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Content.Client._RMC14.Vehicle.Supply;

public sealed class VehicleSupplyWindowController : IDisposable
{
    private static readonly Color ActivityIdleColor = Color.FromHex("#1E3450");
    private static readonly Color ActivityPrepColor = Color.FromHex("#D6C45A");
    private static readonly Color ActivityRaiseColor = Color.FromHex("#6BC7FF");
    private static readonly Color ActivityLowerColor = Color.FromHex("#E1786A");
    private const float PixelsPerMeter = 32f;

    private readonly IEntityManager _entManager = IoCManager.Resolve<IEntityManager>();
    private readonly SpriteSystem _spriteSystem;
    private readonly VehicleSupplyWindow _window;
    private readonly List<VehicleHardpointLayerState> _previewLayers = new();
    private readonly List<VehicleSupplyPreviewOverlay> _previewOverlays = new();
    private readonly List<PreviewOverlay> _previewOverlayViews = new();
    private bool _disposed;
    private bool _previewDirty;
    private bool _previewOverlaysDirty;
    private bool _activityActive;
    private float _activityTimer;
    private int _activityIndex;
    private Color _activityActiveColor = ActivityRaiseColor;

    private sealed class PreviewOverlay
    {
        public EntityUid Entity;
        public SpriteComponent Sprite = default!;
        public SpriteView View = default!;
        public VehicleSupplyPreviewOverlay Data = default!;
        public int DirectionCount;
    }

    public VehicleSupplyWindowController(VehicleSupplyWindow window)
    {
        _spriteSystem = _entManager.System<SpriteSystem>();
        _window = window;
        _window.FrameUpdated += OnFrameUpdated;
        _window.OnClose += Dispose;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _window.FrameUpdated -= OnFrameUpdated;
        _window.OnClose -= Dispose;
        ClearPreviewOverlays();
    }

    public void RefreshPreview(VehicleSupplyPreviewState? preview, string? previewName)
    {
        if (_disposed)
            return;

        if (preview == null || string.IsNullOrWhiteSpace(preview.VehicleId))
        {
            _window.PreviewTitle.Text = "Vehicle Preview";
            _window.VehiclePreview.SetPrototype(null);
            _previewLayers.Clear();
            _previewDirty = false;
            _previewOverlays.Clear();
            _previewOverlaysDirty = false;
            ClearPreviewOverlays();
            InvalidatePreview();
            return;
        }

        _window.PreviewTitle.Text = string.IsNullOrWhiteSpace(previewName) ? preview.VehicleId : previewName;
        _window.VehiclePreview.SetPrototype(preview.VehicleId);
        _window.VehiclePreview.OverrideDirection = Direction.South;

        _previewLayers.Clear();
        _previewLayers.AddRange(preview.Layers);
        _previewDirty = true;

        _previewOverlays.Clear();
        _previewOverlays.AddRange(preview.Overlays);
        _previewOverlaysDirty = true;

        TryApplyPreviewLayers();
        TryApplyPreviewOverlays();
        InvalidatePreview();
    }

    public void RefreshLiftActivity(VehicleSupplyLiftMode? mode, bool busy)
    {
        if (_disposed)
            return;

        if (busy && mode == VehicleSupplyLiftMode.Preparing)
        {
            _activityActive = true;
            _activityActiveColor = ActivityPrepColor;
        }
        else if (mode == VehicleSupplyLiftMode.Raising)
        {
            _activityActive = true;
            _activityActiveColor = ActivityRaiseColor;
        }
        else if (mode == VehicleSupplyLiftMode.Lowering)
        {
            _activityActive = true;
            _activityActiveColor = ActivityLowerColor;
        }
        else
        {
            _activityActive = false;
        }

        if (!_activityActive)
            ResetLiftActivity();
    }

    private void OnFrameUpdated(float frameTime)
    {
        TryApplyPreviewLayers();
        TryApplyPreviewOverlays();

        if (_previewOverlayViews.Count > 0)
        {
            var direction = _window.VehiclePreview.OverrideDirection ?? Direction.South;
            UpdatePreviewOverlayOffsets(direction);
        }

        UpdateLiftActivity(frameTime);
    }

    private void TryApplyPreviewLayers()
    {
        if (!_previewDirty || _window.VehiclePreview.Entity is not { } entity)
            return;

        var sprite = entity.Comp1;
        foreach (var entry in _previewLayers)
        {
            if (!_spriteSystem.LayerMapTryGet((entity.Owner, sprite), entry.Layer, out var layer, false))
                continue;

            if (string.IsNullOrWhiteSpace(entry.State))
            {
                _spriteSystem.LayerSetVisible((entity.Owner, sprite), layer, false);
                continue;
            }

            _spriteSystem.LayerSetRsiState((entity.Owner, sprite), layer, entry.State);
            _spriteSystem.LayerSetVisible((entity.Owner, sprite), layer, true);
        }

        _previewDirty = false;
    }

    private void TryApplyPreviewOverlays()
    {
        if (!_previewOverlaysDirty)
            return;

        ClearPreviewOverlays();

        if (_window.PreviewContainer == null || _window.VehiclePreview.Entity == null)
            return;

        var direction = _window.VehiclePreview.OverrideDirection ?? Direction.South;
        var ordered = _previewOverlays
            .OrderBy(overlay => overlay.Order)
            .ToList();

        foreach (var overlay in ordered)
        {
            if (string.IsNullOrWhiteSpace(overlay.Rsi) || string.IsNullOrWhiteSpace(overlay.State))
                continue;

            var overlayEntity = _entManager.Spawn(null);
            var sprite = _entManager.AddComponent<SpriteComponent>(overlayEntity);
            var spec = new SpriteSpecifier.Rsi(new ResPath(overlay.Rsi), overlay.State);
            var layer = _spriteSystem.AddLayer((overlayEntity, sprite), spec);

            if (layer >= 0)
                _spriteSystem.LayerSetVisible((overlayEntity, sprite), layer, true);

            var view = new SpriteView
            {
                MinSize = _window.VehiclePreview.MinSize,
                Stretch = _window.VehiclePreview.Stretch,
                VerticalAlignment = _window.VehiclePreview.VerticalAlignment,
                HorizontalAlignment = _window.VehiclePreview.HorizontalAlignment,
                Margin = _window.VehiclePreview.Margin,
                OverrideDirection = direction,
                SpriteOffset = true
            };

            view.SetEntity(overlayEntity);
            _window.PreviewContainer.AddChild(view);
            _previewOverlayViews.Add(new PreviewOverlay
            {
                Entity = overlayEntity,
                Sprite = sprite,
                View = view,
                Data = overlay,
                DirectionCount = _spriteSystem.LayerGetDirectionCount((overlayEntity, sprite), 0)
            });
        }

        UpdatePreviewOverlayOffsets(direction);
        _previewOverlaysDirty = false;
    }

    private void UpdatePreviewOverlayOffsets(Direction direction)
    {
        if (_previewOverlayViews.Count == 0)
            return;

        foreach (var overlay in _previewOverlayViews)
        {
            var effectiveDirection = GetEffectiveDirection(direction, overlay.DirectionCount);
            overlay.View.OverrideDirection = effectiveDirection;
            var offsetPixels = GetOffsetPixels(overlay.Data, effectiveDirection);
            var offsetMeters = offsetPixels / PixelsPerMeter;
            _spriteSystem.SetOffset((overlay.Entity, overlay.Sprite), offsetMeters);
        }
    }

    private void ClearPreviewOverlays()
    {
        foreach (var overlay in _previewOverlayViews)
        {
            if (!overlay.View.Disposed)
                overlay.View.Orphan();

            if (!_entManager.Deleted(overlay.Entity))
                _entManager.DeleteEntity(overlay.Entity);
        }

        _previewOverlayViews.Clear();
    }

    private void ResetLiftActivity()
    {
        SetDotColor(_window.LiftDot1, ActivityIdleColor);
        SetDotColor(_window.LiftDot2, ActivityIdleColor);
        SetDotColor(_window.LiftDot3, ActivityIdleColor);
        _activityTimer = 0f;
        _activityIndex = 0;
    }

    private void UpdateLiftActivity(float frameTime)
    {
        if (!_activityActive)
            return;

        _activityTimer += frameTime;
        if (_activityTimer < 0.2f)
            return;

        _activityTimer = 0f;
        _activityIndex = (_activityIndex + 1) % 3;

        SetDotColor(_window.LiftDot1, _activityIndex == 0 ? _activityActiveColor : ActivityIdleColor);
        SetDotColor(_window.LiftDot2, _activityIndex == 1 ? _activityActiveColor : ActivityIdleColor);
        SetDotColor(_window.LiftDot3, _activityIndex == 2 ? _activityActiveColor : ActivityIdleColor);
    }

    private void InvalidatePreview()
    {
        _window.VehiclePreview.InvalidateMeasure();
        _window.PreviewContainer?.InvalidateMeasure();
    }

    private static Vector2 GetOffsetPixels(VehicleSupplyPreviewOverlay overlay, Direction direction)
    {
        if (!overlay.UseDirectional)
            return overlay.BaseOffset;

        var angle = direction.ToAngle();
        var normalized = angle.Theta % MathHelper.TwoPi;
        if (normalized < 0f)
            normalized += MathHelper.TwoPi;

        var segment = MathHelper.PiOver2;
        var index = (int)Math.Floor(normalized / segment) & 3;
        var t = (float)((normalized - index * segment) / segment);

        var current = overlay.BaseOffset + GetDirectionalOffset(overlay, index);
        var next = overlay.BaseOffset + GetDirectionalOffset(overlay, (index + 1) & 3);
        return Vector2.Lerp(current, next, t);
    }

    private static Direction GetEffectiveDirection(Direction direction, int directionCount)
    {
        if (directionCount <= 1)
            return Direction.South;

        if (directionCount <= 4)
            return direction.Convert(RsiDirectionType.Dir4).Convert();

        return direction;
    }

    private static Vector2 GetDirectionalOffset(VehicleSupplyPreviewOverlay overlay, int index)
    {
        return index switch
        {
            0 => overlay.South,
            1 => overlay.East,
            2 => overlay.North,
            3 => overlay.West,
            _ => Vector2.Zero
        };
    }

    private static void SetDotColor(PanelContainer dot, Color color)
    {
        dot.PanelOverride = new StyleBoxFlat
        {
            BackgroundColor = color,
            BorderColor = Color.FromHex("#2D5E8E"),
            BorderThickness = new Thickness(1)
        };
    }
}
