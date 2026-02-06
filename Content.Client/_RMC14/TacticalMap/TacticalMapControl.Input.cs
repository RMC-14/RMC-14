using System;
using System.Numerics;
using Content.Shared._RMC14.TacticalMap;
using Robust.Client.UserInterface;
using Robust.Shared.Input;
using Robust.Shared.Maths;

namespace Content.Client._RMC14.TacticalMap;

public sealed partial class TacticalMapControl
{
    protected override void KeyBindDown(GUIBoundKeyEventArgs args)
    {
        base.KeyBindDown(args);

        if (args.Function == EngineKeyFunctions.UIClick)
        {
            if (HandleQueenEyeClick(args))
                return;

            if (HandleLabelClick(args))
                return;

            if (HandleBlipClick(args))
                return;

            HandleDrawingClick(args);
        }
        else if (args.Function == EngineKeyFunctions.UIRightClick)
        {
            HandlePanningStart(args);
        }
    }

    private bool HandleQueenEyeClick(GUIBoundKeyEventArgs args)
    {
        if (QueenEyeMode && !Drawing && !IsCanvas)
        {
            Vector2i clickPosition = PositionToIndices(args.RelativePosition);
            OnQueenEyeMove?.Invoke(clickPosition);
            args.Handle();
            return true;
        }
        return false;
    }

    private bool HandleLabelClick(GUIBoundKeyEventArgs args)
    {
        if (!LabelEditMode || !Drawing || CurrentLabelMode == LabelMode.None)
            return false;

        Vector2i? labelPosition = GetLabelAtPosition(args.RelativePosition);
        if (labelPosition != null)
        {
            if (CurrentLabelMode == LabelMode.Tactical)
            {
                _draggingLabel = labelPosition;
                _labelDragStart = LogicalToPixel(args.RelativePosition).Floored();
                _currentDragPosition = LogicalToPixel(args.RelativePosition);
            }
            args.Handle();
            return true;
        }

        Vector2i clickPosition = PositionToIndices(args.RelativePosition);
        ShowLabelInputDialog(clickPosition);
        args.Handle();
        return true;
    }

    private bool HandleBlipClick(GUIBoundKeyEventArgs args)
    {
        var blipIndex = GetBlipIndexAtPosition(args.RelativePosition);
        var allowBlipClick = !QueenEyeMode &&
            (!Drawing || (!StraightLineMode && !SquareMode && !EraserMode && !LabelEditMode));

        if (blipIndex != null && _blips != null && allowBlipClick)
        {
            var blip = _blips[blipIndex.Value];
            OnBlipClicked?.Invoke(blip.Indices);
            var entityId = _blipEntityIds != null && blipIndex.Value < _blipEntityIds.Length
                ? _blipEntityIds[blipIndex.Value]
                : (int?)null;
            OnBlipEntityClicked?.Invoke(blip.Indices, entityId);
            args.Handle();
            return true;
        }
        return false;
    }

    private void HandleDrawingClick(GUIBoundKeyEventArgs args)
    {
        HideTunnelInfo();

        if (Drawing && !LabelEditMode)
        {
            _dragging = true;
            Vector2 startPixel = LogicalToPixel(args.RelativePosition);
            _dragStart = startPixel.Floored();
            _lastDrag = _dragStart;
            _previewEnd = _dragStart;
            _lastErasePosition = null;

            if (EraserMode)
            {
                _lastErasePosition = startPixel;
                TryEraseAt(args.RelativePosition);
            }

            OnUserInteraction?.Invoke();
            args.Handle();
        }
    }

    private bool HandleLabelRightClick(GUIBoundKeyEventArgs args)
    {
        Vector2i? labelPosition = GetTacticalLabelAtPosition(args.RelativePosition);
        if (labelPosition == null)
            return false;

        string? existingText = TacticalLabels.TryGetValue(labelPosition.Value, out var data)
            ? data.Text
            : null;
        if (string.IsNullOrWhiteSpace(existingText))
            return false;

        ShowLabelInputDialog(labelPosition.Value, existingText);
        args.Handle();
        return true;
    }

    private bool HandleBlipRightClick(GUIBoundKeyEventArgs args)
    {
        TacticalMapBlip? clickedBlip = GetBlipAtPosition(args.RelativePosition);
        if (clickedBlip != null && OnBlipRightClicked != null)
        {
            OnBlipRightClicked.Invoke(clickedBlip.Value.Indices, "");
            args.Handle();
            return true;
        }
        return false;
    }

    private void HandleRightClickAction(GUIBoundKeyEventArgs args)
    {
        if (HandleLabelRightClick(args))
            return;

        if (HandleBlipRightClick(args))
            return;

        HandleContextMenuRequest(args);
    }

    private bool HandleContextMenuRequest(GUIBoundKeyEventArgs args)
    {
        if (OnContextMenuRequested == null || Texture == null)
            return false;

        Vector2i clickPosition = PositionToIndices(args.RelativePosition);
        OnContextMenuRequested.Invoke(this, clickPosition, args.PointerLocation.Position);
        args.Handle();
        return true;
    }

    private void HandlePanningStart(GUIBoundKeyEventArgs args)
    {
        HideTunnelInfo();
        _rightClickPanning = true;
        _rightClickMoved = false;
        _rightClickStartPosition = LogicalToPixel(args.RelativePosition);
        _lastPanPosition = _rightClickStartPosition;
        args.Handle();
    }

    protected override void KeyBindUp(GUIBoundKeyEventArgs args)
    {
        base.KeyBindUp(args);

        if (args.Function == EngineKeyFunctions.UIClick)
        {
            if (_draggingLabel != null && _labelDragStart != null && _currentDragPosition != null)
            {
                Vector2 currentPixelPos = LogicalToPixel(args.RelativePosition);
                Vector2 dragDiff = currentPixelPos - _labelDragStart.Value;

                if (dragDiff.Length() > MinDragDistance)
                {
                    Vector2i newPosition = PositionToIndices(args.RelativePosition);
                    OnMoveLabel?.Invoke(_draggingLabel.Value, newPosition);
                }
                else
                {
                    string? existingText = GetLabelAt(_draggingLabel.Value);
                    if (existingText != null)
                    {
                        ShowLabelInputDialog(_draggingLabel.Value, existingText);
                    }
                }

                _draggingLabel = null;
                _labelDragStart = null;
                _currentDragPosition = null;
                args.Handle();
            }
            else if (_dragging && Drawing && !LabelEditMode)
            {
                if (!EraserMode && _dragStart != null && (StraightLineMode || SquareMode))
                {
                    Vector2i currentPos = LogicalToPixel(args.RelativePosition).Floored();
                    Vector2i diff = currentPos - _dragStart.Value;

                    if (diff.Length >= MinDragDistance)
                    {
                        Vector2i startIndices = PositionToIndices(PixelToLogical(new Vector2(_dragStart.Value.X, _dragStart.Value.Y)));
                        Vector2i endIndices = PositionToIndices(args.RelativePosition);

                        if (StraightLineMode)
                        {
                            endIndices = SnapToStraightLine(startIndices, endIndices);
                            Vector2 lineStart = ConvertIndicesToLineCoordinates(startIndices);
                            Vector2 lineEnd = ConvertIndicesToLineCoordinates(endIndices);
                            AddLineToCanvas(lineStart, lineEnd, smooth: false);
                        }
                        else if (SquareMode)
                        {
                            AddSquareToCanvas(startIndices, endIndices);
                        }
                    }
                }

                _dragging = false;
                _lastDrag = null;
                _dragStart = null;
                _previewEnd = null;
                _lastErasePosition = null;
                args.Handle();
            }
        }
        else if (args.Function == EngineKeyFunctions.UIRightClick)
        {
            if (!_rightClickMoved)
                HandleRightClickAction(args);

            _rightClickPanning = false;
            _lastPanPosition = null;
            _rightClickStartPosition = null;
            _rightClickMoved = false;
            args.Handle();
        }
    }

    protected override void MouseMove(GUIMouseMoveEventArgs args)
    {
        base.MouseMove(args);

        _lastMousePosition = args.RelativePosition;

        if (_draggingLabel != null)
        {
            _currentDragPosition = LogicalToPixel(args.RelativePosition);
            args.Handle();
        }
        else if (_dragging && Drawing && !LabelEditMode)
        {
            Vector2 currentPixelPos = LogicalToPixel(args.RelativePosition);
            Vector2i currentPos = currentPixelPos.Floored();

            if (EraserMode)
            {
                if (_lastErasePosition == null ||
                    (currentPixelPos - _lastErasePosition.Value).Length() >= MinEraserSegmentPixels)
                {
                    TryEraseAt(args.RelativePosition);
                    _lastErasePosition = currentPixelPos;
                }
            }
            else if (StraightLineMode || SquareMode)
            {
                _previewEnd = currentPos;
            }
            else
            {
                if (_lastDrag != null)
                {
                    Vector2i diff = currentPos - _lastDrag.Value;
                    if (diff.Length >= MinDragDistance)
                    {
                        if (Texture == null)
                            return;

                        (_, Vector2 actualTopLeft, float overlayScale) = GetDrawParameters();
                        float minSegment = MinFreehandSegmentPixels / Math.Max(overlayScale, 0.001f);

                        Vector2 lineStart = PositionToLineCoordinatesFloat(
                            PixelToLogical(new Vector2(_lastDrag.Value.X, _lastDrag.Value.Y)),
                            actualTopLeft,
                            overlayScale);
                        Vector2 lineEnd = PositionToLineCoordinatesFloat(
                            args.RelativePosition,
                            actualTopLeft,
                            overlayScale);

                        Vector2 delta = lineEnd - lineStart;
                        float distance = delta.Length();
                        if (distance >= minSegment)
                        {
                            int steps = (int)MathF.Ceiling(distance / minSegment);
                            Vector2 step = delta / steps;
                            Vector2 prev = lineStart;
                            for (int i = 1; i <= steps; i++)
                            {
                                Vector2 next = lineStart + step * i;
                                AddLineSegment(prev, next);
                                prev = next;
                            }
                        }
                        _lastDrag = currentPos;
                    }
                }
            }
            args.Handle();
        }
        else if (_rightClickPanning && _lastPanPosition != null)
        {
            Vector2 currentPixelPos = LogicalToPixel(args.RelativePosition);

            if (_rightClickStartPosition == null)
            {
                _rightClickStartPosition = currentPixelPos;
                _lastPanPosition = currentPixelPos;
            }

            if (!_rightClickMoved)
            {
                if ((currentPixelPos - _rightClickStartPosition.Value).Length() < MinDragDistance)
                    return;

                _rightClickMoved = true;
                _lastPanPosition = currentPixelPos;
            }

            Vector2 panDelta = currentPixelPos - _lastPanPosition.Value;

            _panOffset += panDelta;
            ApplyViewSettings();
            NotifyViewChanged();

            _lastPanPosition = currentPixelPos;
            args.Handle();
        }

        UpdateHoverInfo(args.RelativePosition);
    }

    protected override void MouseExited()
    {
        base.MouseExited();
        _lastMousePosition = null;
        ClearHoverInfo();
    }

    protected override void MouseWheel(GUIMouseWheelEventArgs args)
    {
        base.MouseWheel(args);

        if (Texture == null)
            return;

        Vector2 mousePixelPos = LogicalToPixel(args.RelativePosition);
        (Vector2 oldActualSize, Vector2 oldActualTopLeft, float oldOverlayScale) = GetDrawParameters();

        float oldZoom = _zoomFactor;

        if (args.Delta.Y > 0)
            _zoomFactor *= ZoomSpeed;
        else if (args.Delta.Y < 0)
            _zoomFactor /= ZoomSpeed;

        _zoomFactor = Math.Clamp(_zoomFactor, MinZoom, MaxZoom);

        if (Math.Abs(_zoomFactor - oldZoom) > 0.001f)
        {
            Vector2 relativeToTexture = (mousePixelPos - oldActualTopLeft) / oldOverlayScale;

            ApplyViewSettings();

            (Vector2 newActualSize, Vector2 newActualTopLeft, float newOverlayScale) = GetDrawParameters();
            Vector2 newMousePos = relativeToTexture * newOverlayScale + newActualTopLeft;
            Vector2 mouseDelta = mousePixelPos - newMousePos;

            _panOffset += mouseDelta;
            ApplyViewSettings();
            NotifyViewChanged();
        }

        args.Handle();
    }

    private TacticalMapBlip? GetBlipAtPosition(Vector2 controlPosition)
    {
        var index = GetBlipIndexAtPosition(controlPosition);
        if (index == null || _blips == null)
            return null;

        return _blips[index.Value];
    }

    private int? GetBlipIndexAtPosition(Vector2 controlPosition)
    {
        if (_blips == null || Texture == null)
            return null;

        Vector2 pixelPosition = LogicalToPixel(controlPosition);
        (Vector2 actualSize, Vector2 actualTopLeft, float overlayScale) = GetDrawParameters();
        float clickTolerance = ClickTolerance * overlayScale;

        for (var i = 0; i < _blips.Length; i++)
        {
            var blip = _blips[i];
            Vector2 blipPosition = IndicesToPosition(blip.Indices) * overlayScale + actualTopLeft;
            float scaledBlipSize = GetScaledBlipSize(overlayScale);

            UIBox2 blipRect = UIBox2.FromDimensions(
                blipPosition - new Vector2(clickTolerance, clickTolerance),
                new Vector2(scaledBlipSize + clickTolerance * 2, scaledBlipSize + clickTolerance * 2)
            );

            if (blipRect.Contains(pixelPosition))
                return i;
        }

        return null;
    }
}
