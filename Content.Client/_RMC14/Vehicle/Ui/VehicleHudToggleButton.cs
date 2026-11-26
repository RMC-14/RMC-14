using System.Numerics;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Utility;

namespace Content.Client._RMC14.Vehicle.Ui;

public sealed class VehicleHudToggleButton : ContainerButton
{
    public static readonly ResPath IconRsi = new("/Textures/_RMC14/Interface/vehicle_hud.rsi");

    private readonly SpriteSystem _sprite;
    private readonly TextureRect _icon;
    private readonly TextureRect _overlay;

    public bool Enabled { get; private set; }

    public VehicleHudToggleButton()
    {
        _sprite = IoCManager.Resolve<IEntityManager>().System<SpriteSystem>();

        MinSize = new Vector2(33, 33);
        VerticalAlignment = VAlignment.Center;
        StyleBoxOverride = new StyleBoxEmpty();

        _icon = new TextureRect
        {
            TextureScale = new Vector2(3, 3),
            Stretch = TextureRect.StretchMode.KeepCentered,
        };

        _overlay = new TextureRect
        {
            TextureScale = new Vector2(3, 3),
            Stretch = TextureRect.StretchMode.KeepCentered,
            Visible = false,
        };

        AddChild(_icon);
        AddChild(_overlay);
    }

    public void SetIcon(SpriteSpecifier icon)
    {
        _icon.Texture = _sprite.Frame0(icon);
    }

    public void SetOverlay(SpriteSpecifier overlay)
    {
        _overlay.Texture = _sprite.Frame0(overlay);
    }

    public void Update(bool enabled, bool canToggle, string tooltip)
    {
        Enabled = enabled;
        _overlay.Visible = enabled;
        Disabled = !canToggle;
        Modulate = canToggle ? Color.White : Color.White.WithAlpha(0.5f);
        ToolTip = tooltip;
    }
}
