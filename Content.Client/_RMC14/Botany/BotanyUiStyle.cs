using System.Numerics;
using Content.Client._RMC14.UserInterface;
using Content.Client.Resources;
using Content.Shared._RMC14.Botany;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Maths;

namespace Content.Client._RMC14.Botany;

internal static class BotanyUiStyle
{
    public static readonly Color HeaderColor = Color.FromHex("#12141A");
    public static readonly Color GreenColor = Color.FromHex("#4CAF50");
    public static readonly Color TanColor = Color.FromHex("#ffb950");
    public static readonly Color RedColor = Color.FromHex("#8B2020");
    public static readonly Color ActionColor = Color.FromHex("#d6d6d6");

    public static readonly Color MetricHealthColor = Color.FromHex("#E06C75");
    public static readonly Color MetricGrowthColor = Color.FromHex("#98C379");
    public static readonly Color MetricTemperatureColor = Color.FromHex("#E06C75");
    public static readonly Color MetricPressureColor = Color.FromHex("#61AFEF");
    public static readonly Color MetricWaterColor = Color.FromHex("#56B6C2");
    public static readonly Color MetricNutrientsColor = Color.FromHex("#E5C07B");
    public static readonly Color MetricWeedsColor = Color.FromHex("#7AAB55");
    public static readonly Color MetricPestsColor = Color.FromHex("#D19A66");
    public static readonly Color MetricToxinsColor = Color.FromHex("#C678DD");

    public static readonly Color RowColorA = Color.FromHex("#1B1B1E");
    public static readonly Color RowColorB = Color.FromHex("#202025");

    public static StyleBoxFlat Flat(Color color)
    {
        return new StyleBoxFlat
        {
            BackgroundColor = color,
            ContentMarginLeftOverride = 6,
            ContentMarginRightOverride = 6,
            ContentMarginTopOverride = 1,
            ContentMarginBottomOverride = 1,
        };
    }

    public static Color GetRowColor(int rowIndex)
    {
        return rowIndex % 2 == 0 ? RowColorA : RowColorB;
    }

    public static void Header(PanelContainer panel)
    {
        panel.PanelOverride = Flat(HeaderColor);
    }

    public static string ShortSeedName(string name)
    {
        const string prefix = "packet of ";
        return name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            ? name[prefix.Length..]
            : name;
    }

    public static void AddStatRow(GridContainer grid, string name, string value)
    {
        grid.AddChild(new Label { Text = name, StyleClasses = { "LabelSubText" } });
        grid.AddChild(new Label { Text = value });
    }

    public static void SetStatRows(GridContainer grid, IEnumerable<(string Name, string Value)> rows)
    {
        grid.DisposeAllChildren();
        foreach (var (name, value) in rows)
        {
            AddStatRow(grid, name, value);
        }
    }

    public static string GetHarvestLabel(HarvestType type)
    {
        return type switch
        {
            HarvestType.Repeat => "Repeating",
            HarvestType.SelfHarvest => "Self-harvesting",
            _ => "No repeat",
        };
    }

    public static void StatusRow(PanelContainer panel, Label label, string text, Color color)
    {
        label.Text = text;
        label.FontColorOverride = color == RedColor ? Color.White : Color.Black;
        panel.PanelOverride = Flat(color);
    }

    public static void ClearStatusRow(PanelContainer panel, Label label)
    {
        label.Text = string.Empty;
        panel.PanelOverride = Flat(HeaderColor);
    }

    public static IconHandle IconButton(
        IResourceCache resourceCache,
        Button button,
        string texturePath,
        string text,
        Color? color = null,
        bool bordered = false,
        Control.HAlignment alignment = Control.HAlignment.Center)
    {
        button.Text = null;
        button.StyleBoxOverride = bordered
            ? new StyleBoxFlat
            {
                BackgroundColor = color ?? TanColor,
                BorderColor = Color.Black,
                BorderThickness = new Thickness(1),
                ContentMarginLeftOverride = 10,
                ContentMarginRightOverride = 10,
                ContentMarginTopOverride = 1,
                ContentMarginBottomOverride = 1,
            }
            : Flat(color ?? TanColor);
        button.ModulateSelfOverride = Color.White;

        var row = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            HorizontalAlignment = alignment,
            VerticalAlignment = Control.VAlignment.Center,
            SeparationOverride = 6,
        };

        var texture = new TextureRect
        {
            Stretch = TextureRect.StretchMode.KeepAspectCentered,
            SetSize = new Vector2(14, 14),
            VerticalAlignment = Control.VAlignment.Center,
            ModulateSelfOverride = RMCIcons.Idle,
        };
        row.AddChild(texture);

        var label = new Label
        {
            Text = text,
            FontColorOverride = Color.Black,
            VerticalAlignment = Control.VAlignment.Center,
        };
        row.AddChild(label);

        button.AddChild(row);

        var handle = new IconHandle(resourceCache, texture, label);
        handle.SetPath(texturePath);

        button.OnMouseEntered += _ => handle.SetHovered(true);
        button.OnMouseExited += _ => handle.SetHovered(false);

        return handle;
    }

    public sealed class IconHandle
    {
        private readonly IResourceCache _resourceCache;
        private readonly TextureRect _texture;
        public readonly Label Label;

        public IconHandle(IResourceCache resourceCache, TextureRect texture, Label label)
        {
            _resourceCache = resourceCache;
            _texture = texture;
            Label = label;
        }

        public void SetPath(string path)
        {
            _texture.Texture = _resourceCache.GetTexture(path);
        }

        public void SetHovered(bool hovered)
        {
            _texture.ModulateSelfOverride = hovered ? RMCIcons.Hover : RMCIcons.Idle;
        }
    }
}
