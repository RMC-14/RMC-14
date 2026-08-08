using System.Numerics;
using Content.Shared._RMC14.Ghost;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Utility;

namespace Content.Client._RMC14.UserInterface.Systems.Ghost.Controls;

internal sealed class RMCGhostTargetButton : Button
{
    private static readonly ResPath HealthRsi = new("/Textures/_RMC14/Interface/health_hud.rsi");
    private static readonly Color ColoredBorderColor = Color.FromHex("#2185d0");
    private static readonly Color NeutralBorderColor = Color.FromHex("#66717f");
    private static readonly Color TextColor = Color.FromHex("#f0f0f0");
    private static readonly Color NormalBackgroundColor = Color.FromHex("#25282d");
    private static readonly Color HoverBackgroundColor = Color.FromHex("#3b4652");
    private static readonly Color PressedBackgroundColor = Color.FromHex("#465667");

    private readonly StyleBoxTexture _borderStyle;
    private readonly StyleBoxTexture _backgroundStyle;
    private readonly TextureRect _healthIcon;
    private readonly PanelContainer _tacticalPanel;
    private readonly TextureRect _tacticalBackground;
    private readonly TextureRect _tacticalIcon;
    private readonly Label _nameLabel;
    private readonly BoxContainer _followerCounter;
    private readonly Label _followerCount;

    private RMCGhostTargetEntry _entry;
    private bool _coloredSection;
    private bool _initialized;

    public event Action<NetEntity>? TargetPressed;

    public RMCGhostTargetEntry Entry => _entry;

    public RMCGhostTargetButton(IResourceCache resourceCache)
    {
        _borderStyle = RMCGhostTargetStyles.CreateRoundedBox(resourceCache, NeutralBorderColor);
        _borderStyle.SetContentMarginOverride(StyleBox.Margin.All, 1);
        StyleBoxOverride = _borderStyle;
        ModulateSelfOverride = Color.White;
        HorizontalAlignment = HAlignment.Left;
        VerticalAlignment = VAlignment.Top;
        RectClipContent = true;

        OnPressed += _ => TargetPressed?.Invoke(_entry.Entity);
        OnMouseEntered += _ => SetInteractionStyle(pressed: false, hovered: true);
        OnMouseExited += _ => SetInteractionStyle(pressed: false, hovered: false);
        OnButtonDown += _ => SetInteractionStyle(pressed: true, hovered: true);
        OnButtonUp += _ => SetInteractionStyle(pressed: false, hovered: IsHovered);

        var buttonContainer = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            HorizontalAlignment = HAlignment.Left,
            VerticalAlignment = VAlignment.Center,
        };

        _healthIcon = new TextureRect
        {
            HorizontalAlignment = HAlignment.Center,
            VerticalAlignment = VAlignment.Center,
            Stretch = TextureRect.StretchMode.KeepAspectCentered,
            MinSize = new Vector2(10, 10),
            MaxSize = new Vector2(10, 10),
            Margin = new Thickness(0, 0, 4, 0),
            Visible = false,
        };
        buttonContainer.AddChild(_healthIcon);

        _tacticalPanel = new PanelContainer
        {
            Margin = new Thickness(0, 0, 4, 0),
            Visible = false,
        };
        _tacticalBackground = CreateTacticalIcon();
        _tacticalIcon = CreateTacticalIcon();
        _tacticalPanel.AddChild(_tacticalBackground);
        _tacticalPanel.AddChild(_tacticalIcon);
        buttonContainer.AddChild(_tacticalPanel);

        _nameLabel = new Label
        {
            HorizontalAlignment = HAlignment.Left,
            ClipText = false,
            StyleClasses = { "LabelSmall" },
        };
        buttonContainer.AddChild(_nameLabel);

        _followerCounter = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            HorizontalAlignment = HAlignment.Right,
            VerticalAlignment = VAlignment.Center,
            Margin = new Thickness(2, 0, 0, 0),
            Visible = false,
        };
        _followerCounter.AddChild(new TextureRect
        {
            HorizontalAlignment = HAlignment.Center,
            VerticalAlignment = VAlignment.Center,
            TexturePath = "/Textures/_RMC14/Interface/ghost_counter.svg.96dpi.png",
            Stretch = TextureRect.StretchMode.KeepAspectCentered,
            MinSize = new Vector2(13, 13),
            MaxSize = new Vector2(13, 13),
        });
        _followerCount = new Label
        {
            HorizontalAlignment = HAlignment.Center,
            VerticalAlignment = VAlignment.Center,
            Margin = new Thickness(2, 0, 0, 0),
            StyleClasses = { "LabelSmall" },
        };
        _followerCounter.AddChild(_followerCount);
        buttonContainer.AddChild(_followerCounter);

        _backgroundStyle = RMCGhostTargetStyles.CreateRoundedBox(
            resourceCache,
            NormalBackgroundColor,
            inset: true);
        _backgroundStyle.SetContentMarginOverride(StyleBox.Margin.Horizontal, 5);
        _backgroundStyle.SetContentMarginOverride(StyleBox.Margin.Vertical, 2);
        var background = new PanelContainer
        {
            PanelOverride = _backgroundStyle,
        };
        background.AddChild(buttonContainer);
        AddChild(background);
    }

    public void Update(RMCGhostTargetEntry entry, SpriteSystem spriteSystem, bool coloredSection)
    {
        if (_initialized && EntriesEqual(_entry, entry) && _coloredSection == coloredSection)
            return;

        var previous = _entry;
        var firstUpdate = !_initialized;

        if (firstUpdate || _coloredSection != coloredSection)
        {
            _coloredSection = coloredSection;
            _nameLabel.Modulate = TextColor;
            _followerCount.Modulate = TextColor;
            SetInteractionStyle(pressed: false, hovered: IsHovered);
        }

        if (firstUpdate ||
            previous.DisplayName != entry.DisplayName ||
            previous.DisplayJob != entry.DisplayJob)
        {
            Name = entry.DisplayName;
            _nameLabel.Text = entry.DisplayName;
        }

        if (firstUpdate ||
            previous.HealthState != entry.HealthState ||
            previous.Flags != entry.Flags)
        {
            var health = GetHealthIcon(entry.HealthState);
            _healthIcon.Visible = !entry.IsWarpPoint && health != null;
            if (health != null)
                _healthIcon.Texture = spriteSystem.Frame0(health);
        }

        if (firstUpdate ||
            !Equals(previous.TacticalIcon, entry.TacticalIcon) ||
            !Equals(previous.TacticalBackground, entry.TacticalBackground) ||
            previous.Flags != entry.Flags)
        {
            _tacticalPanel.Visible = !entry.IsWarpPoint && entry.TacticalIcon != null;
            _tacticalIcon.Visible = entry.TacticalIcon != null;
            if (entry.TacticalIcon != null)
                _tacticalIcon.Texture = spriteSystem.Frame0(entry.TacticalIcon);

            _tacticalBackground.Visible = entry.TacticalBackground != null;
            if (entry.TacticalBackground != null)
                _tacticalBackground.Texture = spriteSystem.Frame0(entry.TacticalBackground);
        }

        if (firstUpdate ||
            previous.FollowerCount != entry.FollowerCount ||
            previous.Flags != entry.Flags)
        {
            _followerCounter.Visible = !entry.IsWarpPoint && entry.FollowerCount > 0;
            _followerCount.Text = entry.FollowerCount.ToString();
        }

        if (firstUpdate ||
            previous.DisplayName != entry.DisplayName ||
            previous.DisplayJob != entry.DisplayJob ||
            previous.TooltipJobKind != entry.TooltipJobKind ||
            previous.HealthState != entry.HealthState ||
            previous.HealthPercent != entry.HealthPercent ||
            previous.Flags != entry.Flags)
        {
            ToolTip = entry.IsWarpPoint ? null : GetTooltip(entry);
        }

        _initialized = true;
        _entry = entry;
    }

    private static bool EntriesEqual(RMCGhostTargetEntry left, RMCGhostTargetEntry right)
    {
        return left.Entity == right.Entity &&
               left.DisplayName == right.DisplayName &&
               left.DisplayJob == right.DisplayJob &&
               left.Flags == right.Flags &&
               left.FollowerCount == right.FollowerCount &&
               left.HealthState == right.HealthState &&
               left.HealthPercent == right.HealthPercent &&
               Equals(left.TacticalIcon, right.TacticalIcon) &&
               Equals(left.TacticalBackground, right.TacticalBackground) &&
               left.TooltipJobKind == right.TooltipJobKind;
    }

    private static TextureRect CreateTacticalIcon()
    {
        return new TextureRect
        {
            HorizontalAlignment = HAlignment.Center,
            VerticalAlignment = VAlignment.Center,
            Stretch = TextureRect.StretchMode.KeepAspectCentered,
            MinSize = new Vector2(16, 16),
            MaxSize = new Vector2(16, 16),
            Visible = false,
        };
    }

    private static SpriteSpecifier.Rsi? GetHealthIcon(RMCGhostTargetHealthState state)
    {
        var iconState = state switch
        {
            RMCGhostTargetHealthState.High => "health_high",
            RMCGhostTargetHealthState.Medium => "health_medium",
            RMCGhostTargetHealthState.Low => "health_low",
            _ => null,
        };

        return iconState == null
            ? null
            : new SpriteSpecifier.Rsi(HealthRsi, iconState);
    }

    private static string GetTooltip(RMCGhostTargetEntry entry)
    {
        var tooltip = Loc.GetString(
            "rmc-ghost-target-window-tooltip-name",
            ("name", entry.DisplayName));
        if (entry.DisplayJob != null)
        {
            tooltip += entry.TooltipJobKind == RMCGhostTargetTooltipJobKind.Caste
                ? $"\n{Loc.GetString("rmc-ghost-target-window-tooltip-caste", ("caste", entry.DisplayJob))}"
                : $"\n{Loc.GetString("rmc-ghost-target-window-tooltip-job", ("job", entry.DisplayJob))}";
        }

        if (entry.HealthState != RMCGhostTargetHealthState.None)
        {
            tooltip += $"\n{Loc.GetString(
                "rmc-ghost-target-window-tooltip-health",
                ("health", entry.HealthPercent))}";
        }

        return tooltip;
    }

    private void SetInteractionStyle(bool pressed, bool hovered)
    {
        _borderStyle.Modulate = _coloredSection
            ? ColoredBorderColor
            : NeutralBorderColor;
        _backgroundStyle.Modulate = pressed
            ? PressedBackgroundColor
            : hovered
                ? HoverBackgroundColor
                : NormalBackgroundColor;
    }
}
