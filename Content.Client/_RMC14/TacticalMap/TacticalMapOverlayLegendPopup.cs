using System.Numerics;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Content.Client._RMC14.TacticalMap;

public sealed class TacticalMapOverlayLegendPopup : Popup
{
    private static readonly ResPath AreaInfoRsiPath = new("/Textures/_RMC14/Structures/Machines/ceiling.rsi");

    private readonly PanelContainer _background;
    private readonly BoxContainer _rows;
    private readonly SpriteSystem _spriteSystem;

    public TacticalMapOverlayLegendPopup()
    {
        _spriteSystem = IoCManager.Resolve<IEntityManager>().System<SpriteSystem>();

        _background = new PanelContainer
        {
            PanelOverride = new StyleBoxFlat
            {
                BackgroundColor = Color.FromHex("#0B0F14").WithAlpha(0.75f),
                BorderColor = Color.FromHex("#0B0F14").WithAlpha(0.1f),
                BorderThickness = new Thickness(1)
            }
        };

        var root = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            Margin = new Thickness(4),
            SeparationOverride = 4
        };

        root.AddChild(new Label
        {
            Text = Loc.GetString("ui-tactical-map-overlay-legend-title"),
            StyleClasses = { "LabelSmall" },
            FontColorOverride = Color.White
        });

        _rows = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            SeparationOverride = 3
        };

        root.AddChild(_rows);
        _background.AddChild(root);
        AddChild(_background);

        UserInterfaceManager.ModalRoot.AddChild(this);
        BuildRows();
    }

    private void BuildRows()
    {
        _rows.RemoveAllChildren();

        AddRow("roof0", "ui-tactical-map-overlay-roof0-desc");
        AddRow("roof1", "ui-tactical-map-overlay-roof1-desc");
        AddRow("roof2", "ui-tactical-map-overlay-roof2-desc");
        AddRow("roof3", "ui-tactical-map-overlay-roof3-desc");
        AddRow("roof4", "ui-tactical-map-overlay-roof4-desc");
    }

    private void AddRow(string state, string locKey)
    {
        var row = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            SeparationOverride = 6
        };

        row.AddChild(new TextureRect
        {
            MinSize = new Vector2(18, 18),
            MaxSize = new Vector2(18, 18),
            Stretch = TextureRect.StretchMode.KeepAspectCentered,
            Texture = _spriteSystem.Frame0(new SpriteSpecifier.Rsi(AreaInfoRsiPath, state))
        });

        row.AddChild(new Label
        {
            Text = Loc.GetString(locKey),
            StyleClasses = { "LabelSmall" },
            FontColorOverride = Color.FromHex("#E5E5E5")
        });

        _rows.AddChild(row);
    }
}
