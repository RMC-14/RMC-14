using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using JetBrains.Annotations;
using Content.Shared._RMC14.Language.Prototypes;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.RichText;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client._RMC14.Chat;

[UsedImplicitly]
public sealed class LanguageIconTag : IMarkupTagHandler
{
    [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public string Name => "langicon";

    public bool TryCreateControl(MarkupNode node, [NotNullWhen(true)] out Control? control)
    {
        if (!node.Attributes.TryGetValue("language", out var languageParameter) ||
            !languageParameter.TryGetString(out var language) ||
            !_prototypeManager.TryIndex<LanguagePrototype>(language, out var prototype) ||
            prototype.LanguageIcon is not { } icon)
        {
            control = null;
            return false;
        }

        var spriteSystem = _entitySystemManager.GetEntitySystem<SpriteSystem>();
        control = new LanguageIconControl(spriteSystem.Frame0(icon));

        return true;
    }

    private sealed class LanguageIconControl : Control
    {
        private const float VerticalOffset = 5f;

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
