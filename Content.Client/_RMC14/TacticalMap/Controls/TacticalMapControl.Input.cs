using System.Numerics;
using Content.Client._RMC14.TacticalMap.UI;
using Content.Shared._RMC14.TacticalMap;
using Robust.Client.UserInterface;
using Robust.Shared.Input;

namespace Content.Client._RMC14.TacticalMap.Controls;

public sealed partial class TacticalMapControl
{
    private bool _toolActive;
    private bool _panning;
    private bool _panMoved;
    private Vector2 _panStart;
    private Vector2 _panLast;
    private Vector2? _mouse;
    private Vector2i? _hover;

    public Vector2? MousePixel => _mouse;

    protected override void KeyBindDown(GUIBoundKeyEventArgs args)
    {
        base.KeyBindDown(args);
        if (!_hasBase)
            return;

        var pixel = args.RelativePixelPosition;
        if (args.Function == EngineKeyFunctions.UIClick)
        {
            UserInteraction?.Invoke();
            if (_tool != null && Draft != null)
            {
                _toolActive = true;
                _tool.Begin(this, pixel);
                args.Handle();
                return;
            }

            if (PointMode)
            {
                PointClicked?.Invoke(PixelToIndices(pixel));
                args.Handle();
                return;
            }

            if (AllowBlipClicks && TryFindBlip(pixel, out var blip))
            {
                BlipClicked?.Invoke(blip.Entity);
                args.Handle();
            }

            return;
        }

        if (args.Function == EngineKeyFunctions.UIRightClick)
        {
            _panning = true;
            _panMoved = false;
            _panStart = pixel;
            _panLast = pixel;
            args.Handle();
        }
    }

    protected override void KeyBindUp(GUIBoundKeyEventArgs args)
    {
        base.KeyBindUp(args);
        var pixel = args.RelativePixelPosition;
        if (args.Function == EngineKeyFunctions.UIClick)
        {
            if (!_toolActive)
                return;

            _toolActive = false;
            _tool?.End(this, pixel);
            args.Handle();
            return;
        }

        if (args.Function != EngineKeyFunctions.UIRightClick || !_panning)
            return;

        _panning = false;
        args.Handle();
        if (_panMoved || !_hasBase)
            return;

        var indices = PixelToIndices(pixel);
        if (TryFindBlip(pixel, out var blip))
        {
            BlipRightClicked?.Invoke(blip.Entity, blip.Blip.Indices);
            return;
        }

        if (_tool != null && Draft != null && TryGetDraftLabelAt(pixel, out var labelTile))
        {
            OpenLabelDialog(labelTile);
            return;
        }

        ContextMenuRequested?.Invoke(indices, GlobalPosition + args.RelativePosition);
    }

    protected override void MouseMove(GUIMouseMoveEventArgs args)
    {
        base.MouseMove(args);
        var pixel = args.RelativePixelPosition;
        _mouse = pixel;

        if (_panning)
        {
            if (!_panMoved && (pixel - _panStart).Length() >= MinDragDistance && GetPanResistance() > 0f)
            {
                _panMoved = true;
                _panLast = pixel;
            }

            if (_panMoved)
            {
                _pan += (pixel - _panLast) * GetPanResistance();
                _panLast = pixel;
                ClampView();
                UserInteraction?.Invoke();
            }
        }

        if (_toolActive)
            _tool?.Move(this, pixel);

        UpdateHover(pixel);
    }

    protected override void MouseExited()
    {
        base.MouseExited();
        _mouse = null;
        if (_hover == null)
            return;

        _hover = null;
        HoverChanged?.Invoke(null);
    }

    protected override void MouseWheel(GUIMouseWheelEventArgs args)
    {
        base.MouseWheel(args);
        if (!_hasBase || args.Delta.Y == 0)
            return;

        ZoomAt(args.RelativePixelPosition, args.Delta.Y > 0);
        UserInteraction?.Invoke();
        args.Handle();
    }

    private void UpdateHover(Vector2 pixel)
    {
        Vector2i? hover = null;
        if (_hasBase)
        {
            var indices = PixelToIndices(pixel);
            if (IsWithinMap(indices))
                hover = indices;
        }

        if (hover == _hover)
            return;

        _hover = hover;
        HoverChanged?.Invoke(hover);
    }

    public void OpenLabelDialog(Vector2i tile)
    {
        if (Draft == null)
            return;

        var draft = Draft;
        TacticalMapLabelData? existing = draft.Labels.TryGetValue(tile, out var label) ? label : null;
        LabelTextDialog.Show(MapTheme,
            existing?.Text ?? string.Empty,
            onConfirmed: text =>
            {
                text = text.Trim();
                if (text.Length > LabelMaxLength)
                    text = text.Substring(0, LabelMaxLength);

                if (text.Length == 0)
                {
                    if (existing != null)
                        draft.SetLabel(tile, null);
                }
                else
                {
                    draft.SetLabel(tile, new TacticalMapLabelData(text, existing?.Color ?? DrawColor));
                }

                NotifyDraftChanged();
            },
            onDeleted: () =>
            {
                draft.SetLabel(tile, null);
                NotifyDraftChanged();
            });
    }
}
