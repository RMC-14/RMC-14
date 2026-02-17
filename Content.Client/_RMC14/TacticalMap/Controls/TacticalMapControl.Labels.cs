using System;
using System.Collections.Generic;
using System.Numerics;
using Content.Shared._RMC14.TacticalMap;
using Robust.Shared.Maths;
using Color = Robust.Shared.Maths.Color;
using Content.Client._RMC14.TacticalMap.UI;

namespace Content.Client._RMC14.TacticalMap.Controls;

public sealed partial class TacticalMapControl
{
    private Vector2i? GetLabelAtPosition(Vector2 controlPosition)
    {
        if (Texture == null)
            return null;

        if (CurrentLabelMode == LabelMode.None)
            return null;

        Vector2 pixelPosition = LogicalToPixel(controlPosition);
        (Vector2 actualSize, Vector2 actualTopLeft, float overlayScale) = GetDrawParameters();
        float clickTolerance = LabelClickTolerance * overlayScale;

        Vector2i? CheckLabels(Dictionary<Vector2i, TacticalMapLabelData> labels, float yOffsetMultiplier)
        {
            foreach ((Vector2i indices, TacticalMapLabelData data) in labels)
            {
                var label = data.Text;
                if (string.IsNullOrWhiteSpace(label))
                    continue;

                Vector2 position = IndicesToPosition(indices) * overlayScale + actualTopLeft;
                position = position with { Y = position.Y - (LabelYOffset + LabelStackOffset * yOffsetMultiplier) * overlayScale };

                float fontSize = Math.Max(LabelMinFontSize, overlayScale * LabelFontScale);
                float textWidth = label.Length * fontSize * 0.6f;
                float textHeight = fontSize;
                Vector2 textSize = new(textWidth, textHeight);
                UIBox2 labelRect = UIBox2.FromDimensions(
                    position - textSize / 2 - new Vector2(clickTolerance),
                    textSize + new Vector2(clickTolerance * 2)
                );

                if (labelRect.Contains(pixelPosition))
                    return indices;
            }

            return null;
        }

        Vector2i? CheckAreaLabels(Dictionary<Vector2i, string> labels, float yOffsetMultiplier)
        {
            foreach ((Vector2i indices, string label) in labels)
            {
                Vector2 position = IndicesToPosition(indices) * overlayScale + actualTopLeft;
                position = position with { Y = position.Y - (LabelYOffset + LabelStackOffset * yOffsetMultiplier) * overlayScale };

                float fontSize = Math.Max(LabelMinFontSize, overlayScale * LabelFontScale);
                float textWidth = label.Length * fontSize * 0.6f;
                float textHeight = fontSize;
                Vector2 textSize = new(textWidth, textHeight);
                UIBox2 labelRect = UIBox2.FromDimensions(
                    position - textSize / 2 - new Vector2(clickTolerance),
                    textSize + new Vector2(clickTolerance * 2)
                );

                if (labelRect.Contains(pixelPosition))
                    return indices;
            }

            return null;
        }

        if (CurrentLabelMode == LabelMode.All)
        {
            return CheckLabels(TacticalLabels, 1f) ?? CheckAreaLabels(_areaLabels, 0f);
        }

        if (CurrentLabelMode == LabelMode.Area)
        {
            return CheckAreaLabels(_areaLabels, 0f);
        }

        return CheckLabels(TacticalLabels, 0f);
    }

    private Vector2i? GetTacticalLabelAtPosition(Vector2 controlPosition)
    {
        if (CurrentLabelMode == LabelMode.Area || CurrentLabelMode == LabelMode.None)
            return null;

        if (Texture == null)
            return null;

        Vector2 pixelPosition = LogicalToPixel(controlPosition);
        (Vector2 actualSize, Vector2 actualTopLeft, float overlayScale) = GetDrawParameters();
        float clickTolerance = LabelClickTolerance * overlayScale;

        foreach ((Vector2i indices, TacticalMapLabelData data) in TacticalLabels)
        {
            var label = data.Text;
            if (string.IsNullOrWhiteSpace(label))
                continue;

            Vector2 position = IndicesToPosition(indices) * overlayScale + actualTopLeft;
            float stackOffset = CurrentLabelMode == LabelMode.All && _areaLabels.ContainsKey(indices)
                ? LabelStackOffset
                : 0f;
            position = position with { Y = position.Y - (LabelYOffset + stackOffset) * overlayScale };

            float fontSize = Math.Max(LabelMinFontSize, overlayScale * LabelFontScale);
            float textWidth = label.Length * fontSize * 0.6f;
            float textHeight = fontSize;
            Vector2 textSize = new(textWidth, textHeight);
            UIBox2 labelRect = UIBox2.FromDimensions(
                position - textSize / 2 - new Vector2(clickTolerance),
                textSize + new Vector2(clickTolerance * 2)
            );

            if (labelRect.Contains(pixelPosition))
                return indices;
        }

        return null;
    }

    private string? GetLabelAt(Vector2i position)
    {
        if (CurrentLabelMode == LabelMode.None)
            return null;

        if (CurrentLabelMode == LabelMode.Tactical)
        {
            return TacticalLabels.TryGetValue(position, out var tacticalData)
                ? tacticalData.Text
                : null;
        }

        if (CurrentLabelMode == LabelMode.Area)
            return _areaLabels.GetValueOrDefault(position);

        if (TacticalLabels.TryGetValue(position, out var data))
            return data.Text;

        return _areaLabels.GetValueOrDefault(position);
    }

    private void ShowLabelInputDialog(Vector2i position, string existingText = "")
    {
        LabelTextDialog.Show(
            position,
            existingText,
            onConfirmed: text =>
            {
                if (string.IsNullOrWhiteSpace(text))
                {
                    if (!string.IsNullOrEmpty(existingText))
                        OnDeleteLabel?.Invoke(position);
                }
                else
                {
                    if (string.IsNullOrEmpty(existingText))
                        OnCreateLabel?.Invoke(position, text);
                    else
                        OnEditLabel?.Invoke(position, text);
                }
            },
            onDeleted: () => OnDeleteLabel?.Invoke(position)
        );
    }

    public void RequestTacticalLabelEditor(Vector2i position)
    {
        var existingText = TacticalLabels.TryGetValue(position, out var data) ? data.Text : string.Empty;
        ShowLabelInputDialog(position, existingText);
    }

    private static Color GetTacticalLabelColor(TacticalMapLabelData data)
    {
        return data.Color.A <= 0f ? DefaultTacticalLabelColor : data.Color;
    }
}
