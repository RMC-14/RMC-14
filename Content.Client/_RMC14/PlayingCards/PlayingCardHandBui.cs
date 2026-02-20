using System.Numerics;
using Content.Client.UserInterface.Controls;
using Content.Shared._RMC14.PlayingCards;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Utility;

namespace Content.Client._RMC14.PlayingCards;

[UsedImplicitly]
public sealed class PlayingCardHandBui : BoundUserInterface
{
    [Dependency] private readonly IClyde _displayManager = default!;
    [Dependency] private readonly IEyeManager _eye = default!;

    private readonly TransformSystem _transform;
    private readonly SpriteSystem _sprite;
    private readonly SharedPlayingCardSystem _cards;

    private PlayingCardHandMenu? _menu;

    public PlayingCardHandBui(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        IoCManager.InjectDependencies(this);

        _transform = EntMan.System<TransformSystem>();
        _sprite = EntMan.System<SpriteSystem>();
        _cards = EntMan.System<SharedPlayingCardSystem>();
    }

    protected override void Open()
    {
        base.Open();
        _menu = this.CreateWindow<PlayingCardHandMenu>();

        if (EntMan.Deleted(Owner))
            return;

        Refresh();
    }

    public void Refresh()
    {
        if (_menu == null)
            return;

        if (!EntMan.TryGetComponent(Owner, out PlayingCardHandComponent? hand))
            return;

        _menu.Cards.Children.Clear();

        var rsiPath = new ResPath("_RMC14/Objects/Fun/playing_cards.rsi");

        for (var i = 0; i < hand.Cards.Count; i++)
        {
            var encoded = hand.Cards[i];
            var (suit, rank) = SharedPlayingCardSystem.DecodeCard(encoded);
            var stateName = SharedPlayingCardSystem.GetCardStateName(suit, rank);

            var suitName = _cards.GetSuitDisplayName(suit);
            var rankName = _cards.GetRankDisplayName(rank);

            var button = new RadialMenuTextureButton
            {
                StyleClasses = { "RadialMenuButton" },
                SetSize = new Vector2(64, 64),
                ToolTip = $"{rankName} of {suitName}",
            };

            var specifier = new SpriteSpecifier.Rsi(rsiPath, stateName);
            var texture = new TextureRect
            {
                TextureScale = new Vector2(2f, 2f),
                Texture = _sprite.Frame0(specifier),
                Stretch = TextureRect.StretchMode.KeepAspectCentered,
            };

            var cardIndex = i;
            button.OnButtonDown += _ =>
            {
                SendPredictedMessage(new PlayingCardHandBuiMsg(cardIndex));
                Close();
            };

            button.AddChild(texture);
            _menu.Cards.AddChild(button);
        }

        var vpSize = _displayManager.ScreenSize;
        var pos = _eye.WorldToScreen(_transform.GetMapCoordinates(Owner).Position) / vpSize;
        _menu.OpenCenteredAt(pos);
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is PlayingCardHandBuiState)
            Refresh();
    }
}
