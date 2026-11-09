using System.Numerics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;

namespace Content.Client._RMC14.Announce;

public sealed class RMCAnnouncementTextLabel : Control
{
    public static readonly Color DefaultShadowColor = Color.Black.WithAlpha(0.85f);

    private readonly RichTextLabel _shadow = new() { MouseFilter = MouseFilterMode.Ignore };
    private readonly RichTextLabel _text = new() { MouseFilter = MouseFilterMode.Ignore };
    private float _shadowOffset = 1;

    public Color ShadowColor = DefaultShadowColor;

    public RMCAnnouncementTextLabel()
    {
        MouseFilter = MouseFilterMode.Ignore;
        AddChild(_shadow);
        AddChild(_text);
    }

    public float ShadowOffset
    {
        get => _shadowOffset;
        set
        {
            _shadowOffset = value;
            InvalidateMeasure();
        }
    }

    public void SetModulate(Color text, Color shadow)
    {
        _text.Modulate = text;
        _shadow.Modulate = shadow;
    }

    public void SetFont(IPrototypeManager prototype, IResourceCache resourceCache, string fontId, int size)
    {
        RMCAnnouncementText.SetFont(_shadow, prototype, resourceCache, fontId, size);
        RMCAnnouncementText.SetFont(_text, prototype, resourceCache, fontId, size);
    }

    public void SetText(string text, Color color)
    {
        SetSegments(new[] { (text, color) });
    }

    public void SetSegments(IReadOnlyList<(string Text, Color Color)> segments)
    {
        var shadow = new (string Text, Color Color)[segments.Count];
        for (var i = 0; i < segments.Count; i++)
        {
            shadow[i] = (segments[i].Text, ShadowColor.WithAlpha(ShadowColor.A * segments[i].Color.A));
        }

        _text.SetMessage(RMCAnnouncementText.Build(segments));
        _shadow.SetMessage(RMCAnnouncementText.Build(shadow));
    }

    protected override Vector2 MeasureOverride(Vector2 availableSize)
    {
        var offset = new Vector2(_shadowOffset);
        var available = Vector2.Max(availableSize - offset, Vector2.Zero);
        _text.Measure(available);
        _shadow.Measure(available);
        return _text.DesiredSize + offset;
    }

    protected override Vector2 ArrangeOverride(Vector2 finalSize)
    {
        var offset = new Vector2(_shadowOffset);
        var size = Vector2.Max(finalSize - offset, Vector2.Zero);
        _shadow.Arrange(UIBox2.FromDimensions(offset, size));
        _text.Arrange(UIBox2.FromDimensions(Vector2.Zero, size));
        return finalSize;
    }
}
