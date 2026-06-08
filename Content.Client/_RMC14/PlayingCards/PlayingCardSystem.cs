using System.Numerics;
using Content.Shared._RMC14.PlayingCards;
using Content.Shared.Hands;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Containers;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Client._RMC14.PlayingCards;

public sealed class PlayingCardSystem : SharedPlayingCardSystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly ISharedPlayerManager _player = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    private const int MaxVisibleCards = 5;
    private const float CardFanOffset = 2f / 32f;
    private static readonly ResPath CardRsiPath = new("_RMC14/Objects/Fun/playing_cards.rsi");

    private EntityQuery<SpriteComponent> _spriteQuery;

    public override void Initialize()
    {
        base.Initialize();

        _spriteQuery = GetEntityQuery<SpriteComponent>();

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
        try
        {
            UpdateCardSprite(ent);
        }
        catch (Exception e)
        {
            Log.Error($"Error in OnCardStateChanged: {e}");
        }
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
        if (!_spriteQuery.TryComp(ent, out var sprite))
            return;

        // Check if card sprite should be hidden (when held by someone else)
        var showFaceUp = ent.Comp.FaceUp;
        if (showFaceUp &&
            _container.TryGetContainingContainer(ent.Owner, out var container) &&
            container.Owner != _player.LocalEntity &&
            Hands.IsHolding(container.Owner, ent.Owner))
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
        try
        {
            UpdateDeckSprite(ent);
        }
        catch (Exception e)
        {
            Log.Error($"Error in OnDeckStateChanged: {e}");
        }
    }

    private void UpdateDeckSprite(Entity<PlayingCardDeckComponent> ent)
    {
        if (!_spriteQuery.TryComp(ent, out var sprite))
            return;

        // Update deck visual based on how many cards remain
        var count = ent.Comp.CardOrder.Count;
        var state = count == 0 ? "deck_empty"
            : count >= ent.Comp.MaxCards ? "deck"
            : "deck_open";

        _sprite.LayerSetRsi((ent.Owner, sprite), 0, CardRsiPath);
        _sprite.LayerSetRsiState((ent.Owner, sprite), 0, state);
    }

    private void OnHandStartup(Entity<PlayingCardHandComponent> ent, ref ComponentStartup args)
    {
        UpdateHandSprite(ent);
    }

    private void OnHandStateChanged(Entity<PlayingCardHandComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        try
        {
            UpdateHandSprite(ent);
            if (Ui.TryGetOpenUi<PlayingCardHandBui>(ent.Owner, PlayingCardHandUi.Key, out var bui))
                bui.Refresh();
        }
        catch (Exception e)
        {
            Log.Error($"Error in OnHandStateChanged: {e}");
        }
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
        if (!_spriteQuery.TryComp(ent, out var sprite))
            return;

        for (var i = MaxVisibleCards - 1; i >= 1; i--)
        {
            var key = $"card_{i}";
            if (_sprite.LayerMapTryGet((ent.Owner, sprite), key, out _, false))
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

        // Calculate starting offset to center the fan
        var totalWidth = (cardsToShow - 1) * CardFanOffset;
        var startOffset = -totalWidth / 2f;

        // Check if card sprite should be hidden (when held by someone else)
        var showFaceUp = ent.Comp.FaceUp;
        if (showFaceUp &&
            _container.TryGetContainingContainer(ent.Owner, out var container) &&
            container.Owner != _player.LocalEntity &&
            Hands.IsHolding(container.Owner, ent.Owner))
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
                _sprite.LayerSetRsi((ent.Owner, sprite), 0, CardRsiPath);
                _sprite.LayerSetRsiState((ent.Owner, sprite), 0, stateName);
                _sprite.LayerSetOffset((ent.Owner, sprite), 0, new Vector2(startOffset, 0));
            }
            else
            {
                // New layer for each additional card
                var key = $"card_{i}";
                var layerIndex = _sprite.AddLayer((ent.Owner, sprite), new SpriteSpecifier.Rsi(CardRsiPath, stateName));
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
