using System.Numerics;
using Content.Shared._RMC14.TacticalMap;
using Robust.Shared.Localization;
using Robust.Shared.Maths;

namespace Content.Client._RMC14.TacticalMap.Controls;

public sealed partial class TacticalMapControl
{
    private void UpdateHoverInfo(Vector2 controlPosition)
    {
        if (OnHoverAreaInfo == null || Texture == null)
            return;

        Vector2i indices = PositionToIndices(controlPosition);
        if (!IsWithinMap(indices))
        {
            ClearHoverInfo();
            return;
        }

        if (_lastHoverIndices == indices)
            return;

        _lastHoverIndices = indices;

        TacticalMapAreaInfo info;
        if (!TryGetAreaInfo(indices, out info))
            info = CreateFallbackAreaInfo(indices);

        OnHoverAreaInfo?.Invoke(info);
    }

    private void ClearHoverInfo()
    {
        if (_lastHoverIndices == null)
            return;

        _lastHoverIndices = null;
        OnHoverAreaInfo?.Invoke(null);
    }

    private bool IsWithinMap(Vector2i indices)
    {
        if (_tileMaskWidth <= 0 || _tileMaskHeight <= 0)
            return true;

        Vector2i drawPosition = GetDrawPosition(indices);
        return drawPosition.X >= 0 && drawPosition.Y >= 0 &&
               drawPosition.X < _tileMaskWidth && drawPosition.Y < _tileMaskHeight;
    }

    private static TacticalMapAreaInfo CreateFallbackAreaInfo(Vector2i indices)
    {
        return new TacticalMapAreaInfo(
            indices,
            Loc.GetString("rmc-tacmap-alert-no-area"),
            null,
            null,
            null,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            null);
    }
}
