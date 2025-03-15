using System.Numerics;
using Content.Shared._RMC14.CCVar;
using Content.Shared._RMC14.IconLabel;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Configuration;

namespace Content.Client._RMC14.Storage;

public sealed class RMCIconLabelsSystem : SharedRMCIconLabelSystem
{
    [Dependency] private readonly IResourceCache _cache = default!;
    [Dependency] private readonly IConfigurationManager _config = default!;

    private Font _font = default!;

    private bool _drawStorageIconLabels;

    public override void Initialize()
    {
        _font = new VectorFont(_cache.GetResource<FontResource>("/Fonts/NotoSans/NotoSans-Regular.ttf"), 8);

        Subs.CVar(_config, RMCCVars.RMCDrawStorageIconLabels, v => _drawStorageIconLabels = v, true);
    }

    public void DrawStorage(EntityUid entity, float uiScale, Vector2 iconPosition, DrawingHandleScreen handle)
    {
        if (!_drawStorageIconLabels)
            return;

        if (!TryComp(entity, out IconLabelComponent? iconLabel))
            return;

        var scale = 2 * uiScale;
        if (iconLabel.LabelTextLocId is null
            || (!Loc.TryGetString(
                iconLabel.LabelTextLocId, out var msg, iconLabel.LabelTextParams.ToArray())
            && !GetLabelForIcon(entity, out msg)))
        {
            return;
        }

        Color.TryFromName(iconLabel.TextColor, out var textColor);

        var charArray = ToLabelShortForm(msg, iconLabel.LabelMaxSize).ToCharArray();

        if (charArray.Length == 0)
            return;

        var textSize = iconLabel.TextSize;

        int thirdCharOffset = charArray.Length > 2
            ? (_font.GetCharMetrics(new System.Text.Rune(charArray[2]), textSize * scale)?.Advance ?? 0)
            : 0;

        var iconLabelPosition = new Vector2(iconPosition.X + scale * iconLabel.StoredOffset.X - thirdCharOffset,
            iconPosition.Y + scale * iconLabel.StoredOffset.Y);


        float sep = 0;
        foreach (var chr in charArray)
        {
            iconLabelPosition.X += sep;
            sep = _font.DrawChar(handle, new System.Text.Rune(chr), iconLabelPosition, textSize * scale, textColor);
        }
    }
}
