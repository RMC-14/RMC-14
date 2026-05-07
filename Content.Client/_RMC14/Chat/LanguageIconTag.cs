using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using JetBrains.Annotations;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.RichText;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Content.Client._RMC14.Chat;

[UsedImplicitly]
public sealed class LanguageIconTag : IMarkupTagHandler
{
    [Dependency] private readonly IResourceCache _resourceCache = default!;

    public string Name => "langicon";

    public bool TryCreateControl(MarkupNode node, [NotNullWhen(true)] out Control? control)
    {
        if (!node.Attributes.TryGetValue("path", out var pathParameter) ||
            !pathParameter.TryGetString(out var path) ||
            !_resourceCache.TryGetResource<TextureResource>(path, out var texture))
        {
            control = null;
            return false;
        }

        control = new LanguageIconControl(texture);

        return true;
    }

    private sealed class LanguageIconControl : Control
    {
        private const float VerticalOffset = 2f;

        private readonly TextureRect _icon;

        public LanguageIconControl(Texture texture)
        {
            _icon = new TextureRect
            {
                Texture = texture,
                TextureScale = Vector2.One * 0.5f,
            };

            AddChild(_icon);
        }

        protected override Vector2 MeasureOverride(Vector2 availableSize)
        {
            _icon.Measure(availableSize);
            var desired = _icon.DesiredSize;
            return new Vector2(desired.X + 4f, desired.Y + VerticalOffset);
        }

        protected override Vector2 ArrangeOverride(Vector2 finalSize)
        {
            _icon.Arrange(UIBox2.FromDimensions(new Vector2(0f, VerticalOffset), _icon.DesiredSize));
            return finalSize;
        }
    }
}
