using System;
using System.Collections.Generic;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.IoC;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Robust.Shared.Utility;
using System.Numerics;
using Content.Client._RMC14.TacticalMap.Controls;

namespace Content.Client._RMC14.TacticalMap.UI;

public sealed class TacticalMapContextPopup : Popup
{
    private readonly PanelContainer _background;
    private readonly BoxContainer _infoContainer;
    private readonly BoxContainer _headerRow;
    private readonly TextureRect _areaIcon;
    private readonly Label _areaNameLabel;
    private readonly TacticalMapButton _createLabelButton;
    private readonly TacticalMapButton _createMortarButton;
    private readonly TacticalMapButton _deleteMortarButton;
    private readonly TacticalMapButton _enterCoordinatesButton;
    private readonly SpriteSystem _spriteSystem;

    private static readonly ResPath AreaInfoRsiPath = new("/Textures/_RMC14/Structures/Machines/ceiling.rsi");

    public Action? OnCreateLabelPressed;
    public Action? OnCreateMortarPressed;
    public Action? OnDeleteMortarPressed;
    public Action? OnEnterCoordinatesPressed;

    public TacticalMapContextPopup()
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
            Margin = new Thickness(2)
        };

        _headerRow = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            SeparationOverride = 3,
        };

        _areaIcon = new TextureRect
        {
            MinSize = new Vector2(18, 18),
            MaxSize = new Vector2(18, 18),
            Stretch = TextureRect.StretchMode.KeepAspectCentered
        };

        _areaNameLabel = new Label
        {
            StyleClasses = { "LabelSmall" },
            FontColorOverride = Color.White
        };

        _infoContainer = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            Margin = new Thickness(0, 2, 0, 2)
        };

        _createLabelButton = new TacticalMapButton
        {
            MinHeight = 20,
            HorizontalExpand = true
        };
        _createLabelButton.Button.OnPressed += _ => OnCreateLabelPressed?.Invoke();

        _createMortarButton = new TacticalMapButton
        {
            MinHeight = 20,
            HorizontalExpand = true
        };
        _createMortarButton.Button.OnPressed += _ => OnCreateMortarPressed?.Invoke();

        _deleteMortarButton = new TacticalMapButton
        {
            MinHeight = 20,
            HorizontalExpand = true
        };
        _deleteMortarButton.Button.OnPressed += _ => OnDeleteMortarPressed?.Invoke();

        _enterCoordinatesButton = new TacticalMapButton
        {
            MinHeight = 20,
            HorizontalExpand = true
        };
        _enterCoordinatesButton.Button.OnPressed += _ => OnEnterCoordinatesPressed?.Invoke();

        _headerRow.AddChild(_areaIcon);
        _headerRow.AddChild(_areaNameLabel);
        root.AddChild(_headerRow);
        root.AddChild(_infoContainer);
        _background.AddChild(root);
        AddChild(_background);

        UserInterfaceManager.ModalRoot.AddChild(this);
    }

    public void SetInfo(
        TacticalMapAreaInfo info,
        bool showCoordinates,
        Vector2i? enteredCoordinates,
        Vector2i? calculatedCoordinates,
        bool showDeleteMortar,
        bool showCoordinateEntry)
    {
        _areaNameLabel.Text = info.AreaName;
        _infoContainer.RemoveAllChildren();

        _areaIcon.Texture = _spriteSystem.Frame0(new SpriteSpecifier.Rsi(AreaInfoRsiPath, GetAreaIconState(info)));

        if (showCoordinates)
            AddInfoLine(Loc.GetString("ui-tactical-map-context-coords", ("x", info.Indices.X), ("y", info.Indices.Y)));

        if (enteredCoordinates != null)
        {
            AddInfoLine(Loc.GetString(
                "ui-tactical-map-context-coords-entered",
                ("x", enteredCoordinates.Value.X),
                ("y", enteredCoordinates.Value.Y)));
        }

        if (calculatedCoordinates != null)
        {
            AddInfoLine(Loc.GetString(
                "ui-tactical-map-context-coords-current",
                ("x", calculatedCoordinates.Value.X),
                ("y", calculatedCoordinates.Value.Y)));
        }

        if (!string.IsNullOrWhiteSpace(info.AreaId))
            AddInfoLine(Loc.GetString("ui-tactical-map-context-id", ("id", info.AreaId)));

        if (!string.IsNullOrWhiteSpace(info.LinkedLz))
            AddInfoLine(Loc.GetString("ui-tactical-map-context-linked-lz", ("lz", info.LinkedLz)));

        if (!string.IsNullOrWhiteSpace(info.TacticalLabel))
            AddInfoLine(Loc.GetString("ui-tactical-map-context-tactical-label", ("label", info.TacticalLabel)), Color.FromHex("#8AB4FF"));

        if (!string.IsNullOrWhiteSpace(info.AreaLabel))
            AddInfoLine(Loc.GetString("ui-tactical-map-context-area-label", ("label", info.AreaLabel)), Color.FromHex("#E5E5E5"));

        List<string> allowed = new();
        List<string> blocked = new();
        if (info.HasArea)
            BuildActionLists(info, out allowed, out blocked);

        var labelKey = string.IsNullOrWhiteSpace(info.TacticalLabel)
            ? "ui-tactical-map-label-dialog-create-title"
            : "ui-tactical-map-label-dialog-edit-title";
        _createLabelButton.Text = Loc.GetString(labelKey);
        _createMortarButton.Text = Loc.GetString("ui-tactical-map-create-mortar");
        _createMortarButton.Disabled = info.HasArea && !info.MortarPlacement;
        _deleteMortarButton.Text = Loc.GetString("ui-tactical-map-remove-mortar");
        _deleteMortarButton.Visible = showDeleteMortar;
        _enterCoordinatesButton.Text = Loc.GetString("ui-tactical-map-enter-coordinates");
        _enterCoordinatesButton.Visible = showCoordinateEntry;
        AddActionRow(allowed, blocked);
    }

    private void AddInfoLine(string text, Color? color = null)
    {
        _infoContainer.AddChild(new Label
        {
            Text = text,
            StyleClasses = { "LabelSmall" },
            FontColorOverride = color ?? Color.FromHex("#E5E5E5")
        });
    }

    private static void BuildActionLists(TacticalMapAreaInfo info, out List<string> allowed, out List<string> blocked)
    {
        allowed = new List<string>();
        blocked = new List<string>();

        AddAction(info.OrbitalBombard, "ui-tactical-map-action-ob", allowed, blocked);
        AddAction(info.Cas, "ui-tactical-map-action-cas", allowed, blocked);
        AddAction(info.SupplyDrop, "ui-tactical-map-action-supply", allowed, blocked);
        AddAction(info.MortarFire, "ui-tactical-map-action-mortar-fire", allowed, blocked);
        AddAction(info.MortarPlacement, "ui-tactical-map-action-mortar-place", allowed, blocked);
        AddAction(info.Lasing, "ui-tactical-map-action-lase", allowed, blocked);
        AddAction(info.Medevac, "ui-tactical-map-action-medevac", allowed, blocked);
        AddAction(info.Paradropping, "ui-tactical-map-action-paradrop", allowed, blocked);
        AddAction(info.Fulton, "ui-tactical-map-action-fulton", allowed, blocked);
        AddAction(info.LandingZone, "ui-tactical-map-action-landing-zone", allowed, blocked);
    }

    private void AddActionRow(List<string> allowed, List<string> blocked)
    {
        var row = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            SeparationOverride = 10,
            Margin = new Thickness(0, 2, 0, 0)
        };

        bool hasActions = allowed.Count > 0 || blocked.Count > 0;
        if (hasActions)
        {
            var actionRow = new BoxContainer
            {
                Orientation = BoxContainer.LayoutOrientation.Horizontal,
                SeparationOverride = 10
            };

            if (allowed.Count > 0)
            {
                actionRow.AddChild(BuildActionColumn(
                    "ui-tactical-map-context-allowed-title",
                    Color.FromHex("#43B581"),
                    allowed,
                    "+",
                    Color.FromHex("#43B581")));
            }

            if (blocked.Count > 0)
            {
                actionRow.AddChild(BuildActionColumn(
                    "ui-tactical-map-context-blocked-title",
                    Color.FromHex("#ED4245"),
                    blocked,
                    "-",
                    Color.FromHex("#ED4245")));
            }

            row.AddChild(actionRow);
        }

        _createLabelButton.Orphan();
        _createMortarButton.Orphan();
        _deleteMortarButton.Orphan();
        _enterCoordinatesButton.Orphan();

        var buttonColumn = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            SeparationOverride = 4,
            Margin = hasActions ? new Thickness(6, 0, 0, 0) : default
        };

        buttonColumn.AddChild(_createLabelButton);
        buttonColumn.AddChild(_createMortarButton);
        if (_deleteMortarButton.Visible)
            buttonColumn.AddChild(_deleteMortarButton);
        if (_enterCoordinatesButton.Visible)
            buttonColumn.AddChild(_enterCoordinatesButton);
        row.AddChild(buttonColumn);

        _infoContainer.AddChild(row);
    }

    private static BoxContainer BuildActionColumn(
        string titleKey,
        Color titleColor,
        List<string> actions,
        string prefix,
        Color itemColor)
    {
        var column = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            SeparationOverride = 1
        };

        column.AddChild(new Label
        {
            Text = Loc.GetString(titleKey),
            StyleClasses = { "LabelSmall" },
            FontColorOverride = titleColor
        });

        foreach (var action in actions)
        {
            column.AddChild(new Label
            {
                Text = $"{prefix} {action}",
                StyleClasses = { "LabelSmall" },
                FontColorOverride = itemColor
            });
        }

        return column;
    }

    private static void AddAction(bool allowedAction, string locKey, List<string> allowed, List<string> blocked)
    {
        var name = Loc.GetString(locKey);
        if (allowedAction)
            allowed.Add(name);
        else
            blocked.Add(name);
    }

    private static string GetAreaIconState(TacticalMapAreaInfo info)
    {
        if (!info.HasArea)
            return "roofnull";

        if (!info.OrbitalBombard)
            return "roof4";

        if (!info.Cas)
            return "roof3";

        if (!info.SupplyDrop || !info.MortarFire)
            return "roof2";

        if (!info.MortarPlacement || !info.Lasing || !info.Medevac || !info.Paradropping)
            return "roof1";

        return "roof0";
    }
}
