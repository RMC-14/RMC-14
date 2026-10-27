using System.Numerics;
using Content.Client.Resources;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.IoC;
using Robust.Shared.Maths;

namespace Content.Client._RMC14.UserInterface;

public sealed class RMCIconRect : TextureRect
{
    private readonly IResourceCache _resourceCache;

    private Color _idleColor = RMCIcons.Idle;
    private Color _hoverColor = RMCIcons.Hover;
    private bool _hovered;

    public RMCIconRect()
    {
        _resourceCache = IoCManager.Resolve<IResourceCache>();
        Stretch = StretchMode.KeepAspectCentered;
        VerticalAlignment = VAlignment.Center;
        UpdateColor();
    }

    public RMCIconRect(string path, float size) : this()
    {
        SetIcon(path);
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
        get => SetSize.X;
        set => SetSize = new Vector2(value, value);
    }

    public void SetIcon(string path)
    {
        Texture = _resourceCache.GetTexture(path);
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
        ModulateSelfOverride = _hovered ? _hoverColor : _idleColor;
    }
}
