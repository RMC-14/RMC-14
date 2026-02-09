using System.Numerics;
using Content.Shared._RMC14.PlayingCards;
using Content.Shared.Hands;
using Content.Shared.Hands.EntitySystems;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Containers;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Client._RMC14.PlayingCards;

public sealed class PlayingCardSystem : SharedPlayingCardSystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly ISharedPlayerManager _player = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    // Maximum number of card sprites to show in hand
    private const int MaxVisibleCards = 5;
    private const float CardFanOffset = 2f / 32f;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlayingCardComponent, AfterAutoHandleStateEvent>(OnCardStateChanged);
        SubscribeLocalEvent<PlayingCardDeckComponent, AfterAutoHandleStateEvent>(OnDeckStateChanged);
        SubscribeLocalEvent<PlayingCardHandComponent, AfterAutoHandleStateEvent>(OnHandStateChanged);

        SubscribeLocalEvent<PlayingCardComponent, ComponentStartup>(OnCardStartup);
        SubscribeLocalEvent<PlayingCardDeckComponent, ComponentStartup>(OnDeckStartup);
        SubscribeLocalEvent<PlayingCardHandComponent, ComponentStartup>(OnHandStartup);

        SubscribeLocalEvent<PlayingCardComponent, GotEquippedHandEvent>(OnCardEquippedHand);
        SubscribeLocalEvent<PlayingCardComponent, GotUnequippedHandEvent>(OnCardUnequippedHand);
        SubscribeLocalEvent<PlayingCardHandComponent, GotEquippedHandEvent>(OnHandEquippedHand);
        SubscribeLocalEvent<PlayingCardHandComponent, GotUnequippedHandEvent>(OnHandUnequippedHand);
    }

    private void OnCardStartup(Entity<PlayingCardComponent> ent, ref ComponentStartup args)
    {
        UpdateCardSprite(ent);
    }

    private void OnCardStateChanged(Entity<PlayingCardComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        UpdateCardSprite(ent);
    }

    private void OnCardEquippedHand(Entity<PlayingCardComponent> ent, ref GotEquippedHandEvent args)
    {
        UpdateCardSprite(ent);
    }

    private void OnCardUnequippedHand(Entity<PlayingCardComponent> ent, ref GotUnequippedHandEvent args)
    {
        UpdateCardSprite(ent);
    }

    private void UpdateCardSprite(Entity<PlayingCardComponent> ent)
    {
        if (!TryComp<SpriteComponent>(ent, out var sprite))
            return;

        // Check if card sprite should be hidden (when held by someone else)
        var showFaceUp = ent.Comp.FaceUp;
        if (showFaceUp &&
            _container.TryGetContainingContainer(ent.Owner, out var container) &&
            container.Owner != _player.LocalEntity &&
            _hands.IsHolding(container.Owner, ent.Owner))
        {
            showFaceUp = false;
        }

        if (showFaceUp)
        {
            var stateName = GetCardStateName(ent.Comp.Suit, ent.Comp.Rank);
            _sprite.LayerSetRsiState((ent.Owner, sprite), 0, stateName);
        }
        else
        {
            _sprite.LayerSetRsiState((ent.Owner, sprite), 0, "back_deck");
        }
    }

    private void OnDeckStartup(Entity<PlayingCardDeckComponent> ent, ref ComponentStartup args)
    {
        UpdateDeckSprite(ent);
    }

    private void OnDeckStateChanged(Entity<PlayingCardDeckComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        UpdateDeckSprite(ent);
    }

    private void UpdateDeckSprite(Entity<PlayingCardDeckComponent> ent)
    {
        if (!TryComp<SpriteComponent>(ent, out var sprite))
            return;

        // Update deck visual based on how many cards remain
        var state = ent.Comp.CardsRemaining switch
        {
            0 => "deck_empty",
            <= 26 => "deck_open",
            _ => "deck"
        };

        _sprite.LayerSetRsiState((ent.Owner, sprite), 0, state);
    }

    private void OnHandStartup(Entity<PlayingCardHandComponent> ent, ref ComponentStartup args)
    {
        UpdateHandSprite(ent);
    }

    private void OnHandStateChanged(Entity<PlayingCardHandComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        UpdateHandSprite(ent);
    }

    private void OnHandEquippedHand(Entity<PlayingCardHandComponent> ent, ref GotEquippedHandEvent args)
    {
        UpdateHandSprite(ent);
    }

    private void OnHandUnequippedHand(Entity<PlayingCardHandComponent> ent, ref GotUnequippedHandEvent args)
    {
        UpdateHandSprite(ent);
    }

    private void UpdateHandSprite(Entity<PlayingCardHandComponent> ent)
    {
        if (!TryComp<SpriteComponent>(ent, out var sprite))
            return;

        // Remove any card layers we previously added in REVERSE order to avoid index shifting issues
        for (var i = MaxVisibleCards - 1; i >= 1; i--)
        {
            var key = $"card_{i}";
            if (_sprite.LayerMapTryGet((ent.Owner, sprite), key, out var layerIndex, false))
            {
                _sprite.RemoveLayer((ent.Owner, sprite), key);
            }
        }

        var cardCount = ent.Comp.Cards.Count;

        if (cardCount == 0)
        {
            _sprite.LayerSetRsiState((ent.Owner, sprite), 0, "empty");
            _sprite.LayerSetOffset((ent.Owner, sprite), 0, Vector2.Zero);
            return;
        }

        // Get the cards to display (limit to MaxVisibleCards to avoid visual clutter)
        var cardsToShow = Math.Min(cardCount, MaxVisibleCards);
        var rsiPath = new ResPath("_RMC14/Objects/Fun/playing_cards.rsi");

        // Calculate starting offset to center the fan
        var totalWidth = (cardsToShow - 1) * CardFanOffset;
        var startOffset = -totalWidth / 2f;

        // Check if card sprite should be hidden (when held by someone else)
        var showFaceUp = ent.Comp.FaceUp;
        if (showFaceUp &&
            _container.TryGetContainingContainer(ent.Owner, out var container) &&
            container.Owner != _player.LocalEntity &&
            _hands.IsHolding(container.Owner, ent.Owner))
        {
            showFaceUp = false;
        }

        for (var i = 0; i < cardsToShow; i++)
        {
            var cardIndex = cardCount - cardsToShow + i;
            var encoded = ent.Comp.Cards[cardIndex];
            var (suit, rank) = DecodeCard(encoded);

            string stateName;
            if (showFaceUp)
            {
                stateName = GetCardStateName(suit, rank);
            }
            else
            {
                stateName = "back_deck";
            }

            if (i == 0)
            {
                // Use the base layer for the first card
                _sprite.LayerSetRsi((ent.Owner, sprite), 0, rsiPath);
                _sprite.LayerSetRsiState((ent.Owner, sprite), 0, stateName);
                _sprite.LayerSetOffset((ent.Owner, sprite), 0, new Vector2(startOffset, 0));
            }
            else
            {
                // New layer for each additional card
                var key = $"card_{i}";
                var layerIndex = _sprite.AddLayer((ent.Owner, sprite), new SpriteSpecifier.Rsi(rsiPath, stateName));
                _sprite.LayerMapSet((ent.Owner, sprite), key, layerIndex);
                _sprite.LayerSetOffset((ent.Owner, sprite), layerIndex, new Vector2(startOffset + i * CardFanOffset, 0));
            }
        }
    }

    public override void FlipCard(Entity<PlayingCardComponent> card, EntityUid user)
    {
        base.FlipCard(card, user);
        UpdateCardSprite(card);
    }

    public override void FlipHand(Entity<PlayingCardHandComponent> hand, EntityUid user)
    {
        base.FlipHand(hand, user);
        UpdateHandSprite(hand);
    }
}
