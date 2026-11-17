using System.Numerics;
using Content.Client.Resources;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.IoC;
using Robust.Shared.Maths;

namespace Content.Client._RMC14.UserInterface;

public sealed class RMCIconRect : Label
{
    private const float GlyphScale = 0.6f;

    private readonly IResourceCache _resourceCache;

    private Color _idleColor = RMCIcons.Idle;
    private Color _hoverColor = RMCIcons.Hover;
    private bool _hovered;
    private float _iconSize;

    public RMCIconRect()
    {
        _resourceCache = IoCManager.Resolve<IResourceCache>();
        Align = AlignMode.Center;
        VAlign = VAlignMode.Center;
        VerticalAlignment = VAlignment.Center;
        IconSize = 18;
        UpdateColor();
    }

    public RMCIconRect(string icon, float size) : this()
    {
        SetIcon(icon);
        IconSize = size;
    }

    public Color IdleColor
    {
        get => _idleColor;
        set
        {
            _idleColor = value;
            UpdateColor();
        }
    }

    public Color HoverColor
    {
        get => _hoverColor;
        set
        {
            _hoverColor = value;
            UpdateColor();
        }
    }

    public float IconSize
    {
        get => _iconSize;
        set
        {
            _iconSize = value;
            SetSize = new Vector2(value, value);
            FontOverride = _resourceCache.GetFont(RMCIcons.FontPath, Math.Max(1, (int) MathF.Round(value * GlyphScale)));
        }
    }

    public void SetIcon(string icon)
    {
        Text = icon;
    }

    public void SetHovered(bool hovered)
    {
        _hovered = hovered;
        UpdateColor();
    }

    public void BindHover(Control control)
    {
        control.OnMouseEntered += _ => SetHovered(true);
        control.OnMouseExited += _ => SetHovered(false);
    }

    private void UpdateColor()
    {
        FontColorOverride = _hovered ? _hoverColor : _idleColor;
    }
}
