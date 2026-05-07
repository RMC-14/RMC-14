using System.Numerics;
using Content.Shared._RMC14.Language.Prototypes;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.IoC;
using Robust.Shared.Utility;

namespace Content.Client.UserInterface.Systems.Language;

public sealed class LanguageButton : ContainerButton
{
    private bool _isSelected;
    private readonly LanguagePrototype _prototype;

    private readonly PanelContainer _background;
    private readonly TextureRect _icon;
    private readonly Label _nameLabel;
    private readonly Label _descriptionLabel;
    private readonly Label _selectedIndicator;

    private readonly StyleBoxFlat _normalStyleBox = new()
    {
        BackgroundColor = Color.FromHex("#25252a"),
        BorderColor = Color.FromHex("#404040"),
        BorderThickness = new Thickness(1),
        ContentMarginTopOverride = 4,
        ContentMarginBottomOverride = 4,
        ContentMarginLeftOverride = 8,
        ContentMarginRightOverride = 8
    };

    private readonly StyleBoxFlat _hoverStyleBox = new()
    {
        BackgroundColor = Color.FromHex("#2A2A2F"),
        BorderColor = Color.FromHex("#505050"),
        BorderThickness = new Thickness(1),
        ContentMarginTopOverride = 4,
        ContentMarginBottomOverride = 4,
        ContentMarginLeftOverride = 8,
        ContentMarginRightOverride = 8
    };

    private readonly StyleBoxFlat _selectedStyleBox = new()
    {
        BackgroundColor = Color.FromHex("#2E5233"),
        BorderColor = Color.FromHex("#4CAF50"),
        BorderThickness = new Thickness(2),
        ContentMarginTopOverride = 4,
        ContentMarginBottomOverride = 4,
        ContentMarginLeftOverride = 8,
        ContentMarginRightOverride = 8
    };

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            _isSelected = value;
            UpdateVisualState();
        }
    }

    public event Action? OnPressed;

    public LanguageButton(LanguagePrototype prototype, bool isSelected)
    {
        _prototype = prototype;
        _isSelected = isSelected;

        HorizontalExpand = true;
        SetHeight = 64;

        _background = new PanelContainer
        {
            PanelOverride = _normalStyleBox
        };
        AddChild(_background);

        var mainContainer = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            HorizontalExpand = true,
            VerticalExpand = true,
            Margin = new Thickness(12, 8)
        };

        _icon = new TextureRect
        {
            SetWidth = 32,
            SetHeight = 32,
            Stretch = TextureRect.StretchMode.KeepAspectCentered,
            Margin = new Thickness(0, 0, 12, 0),
            Visible = false
        };

        if (!string.IsNullOrEmpty(prototype.LanguageIcon))
        {
            var resourceCache = IoCManager.Resolve<IResourceCache>();
            if (resourceCache.TryGetResource<TextureResource>(prototype.LanguageIcon, out var texture))
            {
                _icon.Texture = texture;
                _icon.Visible = true;
            }
        }

        mainContainer.AddChild(_icon);

        var textContainer = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            HorizontalExpand = true,
            VerticalAlignment = Control.VAlignment.Center
        };

        _nameLabel = new Label
        {
            Text = prototype.LocalizedName,
            StyleClasses = { "LabelHeading" },
            Margin = new Thickness(0, 0, 0, 2)
        };

        _descriptionLabel = new Label
        {
            Text = prototype.LocalizedDescription,
            FontColorOverride = Color.Gray,
            StyleClasses = { "LabelSubText" }
        };

        textContainer.AddChild(_nameLabel);
        textContainer.AddChild(_descriptionLabel);
        mainContainer.AddChild(textContainer);

        _selectedIndicator = new Label
        {
            Text = "◆",
            SetWidth = 24,
            VerticalAlignment = Control.VAlignment.Center,
            HorizontalAlignment = Control.HAlignment.Center,
            StyleClasses = { "LabelHeading" },
            Visible = false
        };
        mainContainer.AddChild(_selectedIndicator);

        AddChild(mainContainer);

        UpdateVisualState();
        base.OnPressed += _ => OnPressed?.Invoke();
    }

    private void UpdateVisualState()
    {
        if (_background == null || _nameLabel == null || _selectedIndicator == null)
            return;

        if (_isSelected)
        {
            _background.PanelOverride = _selectedStyleBox;
            _nameLabel.FontColorOverride = Color.LightGreen;
            _selectedIndicator.Visible = true;
            _selectedIndicator.FontColorOverride = Color.LightGreen;
        }
        else
        {
            _background.PanelOverride = DrawMode == DrawModeEnum.Hover ? _hoverStyleBox : _normalStyleBox;
            _nameLabel.FontColorOverride = Color.White;
            _selectedIndicator.Visible = false;
        }
    }

    protected override void DrawModeChanged()
    {
        base.DrawModeChanged();

        if (_background == null)
            return;

        if (!_isSelected)
        {
            _background.PanelOverride = DrawMode == DrawModeEnum.Hover ? _hoverStyleBox : _normalStyleBox;
        }
    }

    public void SetThemeColor(Color themeColor)
    {
        var hsv = Color.ToHsv(themeColor);
        var lighterTheme = Color.FromHsv(hsv with { Z = Math.Min(1.0f, hsv.Z * 1.2f) });

        _normalStyleBox.BackgroundColor = themeColor;
        _hoverStyleBox.BackgroundColor = lighterTheme;
        _selectedStyleBox.BackgroundColor = Color.FromHex("#2E5233");

        UpdateVisualState();
    }
}
