using System.Numerics;
using Content.Shared._RMC14.Ghost;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Utility;

namespace Content.Client._RMC14.UserInterface.Systems.Ghost.Controls;

internal sealed class RMCGhostTargetButton : Button
{
    private const float HoverLightnessPercent = 0.1f;
    private static readonly ResPath HealthRsi = new("/Textures/_RMC14/Interface/health_hud.rsi");

    private readonly TextureRect _healthIcon;
    private readonly PanelContainer _tacticalPanel;
    private readonly TextureRect _tacticalBackground;
    private readonly TextureRect _tacticalIcon;
    private readonly Label _nameLabel;
    private readonly BoxContainer _followerCounter;
    private readonly Label _followerCount;

    private RMCGhostTargetEntry _entry;
    private bool _initialized;

    public event Action<NetEntity>? TargetPressed;

    public string SearchText { get; private set; } = string.Empty;

    public RMCGhostTargetEntry Entry => _entry;

    public RMCGhostTargetButton()
    {
        StyleBoxOverride = new StyleBoxTexture
        {
            PatchMarginTop = 5,
            PatchMarginBottom = 5,
            PatchMarginLeft = 5,
            PatchMarginRight = 5,
            ContentMarginTopOverride = 3,
            ContentMarginLeftOverride = 6,
            ContentMarginRightOverride = 6,
            ContentMarginBottomOverride = 3,
            Padding = new Thickness(2),
        };

        OnPressed += _ => TargetPressed?.Invoke(_entry.Entity);
        OnMouseEntered += _ => SetModulate(AdjustLightness(GetModulate(), HoverLightnessPercent));
        OnMouseExited += _ => SetModulate(Color.White);
        OnButtonDown += _ => SetModulate(Color.FromHex("#3e6c45"));
        OnButtonUp += _ => SetModulate(Color.White);

        var buttonContainer = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            HorizontalAlignment = HAlignment.Center,
            VerticalAlignment = VAlignment.Center,
            HorizontalExpand = true,
        };

        _healthIcon = new TextureRect
        {
            HorizontalAlignment = HAlignment.Center,
            VerticalAlignment = VAlignment.Center,
            Stretch = TextureRect.StretchMode.KeepAspectCentered,
            MinSize = new Vector2(13, 13),
            MaxSize = new Vector2(13, 13),
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

        AddChild(buttonContainer);
    }

    public void Update(RMCGhostTargetEntry entry, SpriteSystem spriteSystem)
    {
        if (_initialized && EntriesEqual(_entry, entry))
            return;

        var previous = _entry;
        var firstUpdate = !_initialized;

        if (StyleBoxOverride is StyleBoxTexture style)
        {
            style.Texture ??= spriteSystem.Frame0(
                new SpriteSpecifier.Texture(
                    new ResPath("/Textures/Interface/Nano/rounded_button.svg.96dpi.png")));
        }

        if (firstUpdate ||
            previous.DisplayName != entry.DisplayName ||
            previous.DisplayJob != entry.DisplayJob)
        {
            Name = entry.DisplayName;
            SearchText = entry.DisplayJob == null
                ? entry.DisplayName
                : $"{entry.DisplayName} {entry.DisplayJob}";
            _nameLabel.Text = TruncateText(entry.DisplayName, 15);
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

    private static string TruncateText(string text, int maxLength)
    {
        return text.Length > maxLength
            ? text[..maxLength] + "..."
            : text;
    }

    private Color GetModulate()
    {
        return StyleBoxOverride is StyleBoxTexture style ? style.Modulate : Color.White;
    }

    private void SetModulate(Color color)
    {
        if (StyleBoxOverride is StyleBoxTexture style)
            style.Modulate = color;
    }

    private static Color AdjustLightness(Color color, float percent)
    {
        var hsv = Color.ToHsv(color);
        hsv.Z = percent > 0
            ? Math.Min(hsv.Z * (1f + percent), 1f)
            : hsv.Z * (1f + percent);
        return Color.FromHsv(hsv);
    }
}
