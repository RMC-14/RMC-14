using Content.Server.Hands.Systems;
using Content.Shared._RMC14.PlayingCards;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Server._RMC14.PlayingCards;

public sealed class PlayingCardSystem : SharedPlayingCardSystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly MetaDataSystem _meta = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private const int StackThreshold = 5;

    protected override void DrawCard(Entity<PlayingCardDeckComponent> deck, EntityUid user)
    {
        if (deck.Comp.CardsRemaining <= 0 || deck.Comp.CardOrder.Count == 0)
        {
            _popup.PopupEntity(Loc.GetString("rmc-playing-card-deck-empty"), deck, user);
            return;
        }

        // Take the top card
        var cardIndex = deck.Comp.CardOrder.Count - 1;
        var encoded = deck.Comp.CardOrder[cardIndex];
        deck.Comp.CardOrder.RemoveAt(cardIndex);
        deck.Comp.CardsRemaining = deck.Comp.CardOrder.Count;
        Dirty(deck);

        var (suit, rank) = DecodeCard(encoded);

        // Spawn the card
        var coords = _transform.GetMapCoordinates(deck);
        var card = Spawn(deck.Comp.CardPrototype, coords);

        if (TryComp<PlayingCardComponent>(card, out var cardComp))
        {
            cardComp.Suit = suit;
            cardComp.Rank = rank;
            cardComp.FaceUp = false;
            Dirty(card, cardComp);
        }

        // Update card appearance
        UpdateCardAppearance(card, suit, rank, false);

        // Give to user
        _hands.TryPickupAnyHand(user, card);

        _popup.PopupEntity(Loc.GetString("rmc-playing-card-draw-deck"), deck, user);
        _audio.PlayPvs(deck.Comp.DrawSound, deck);
    }

    protected override void CombineCards(Entity<PlayingCardComponent> card1, Entity<PlayingCardComponent> card2, EntityUid user)
    {
        // Gets hand holding the card
        _hands.IsHolding(user, card2, out var handId);

        var coords = _transform.GetMapCoordinates(card1);

        // Create a new hand of cards
        var hand = Spawn("RMCPlayingCardHand", coords);
        if (!TryComp<PlayingCardHandComponent>(hand, out var handComp))
        {
            QueueDel(hand);
            return;
        }

        // Add both cards to the hand
        handComp.Cards.Add(EncodeCard(card1.Comp.Suit, card1.Comp.Rank));
        handComp.Cards.Add(EncodeCard(card2.Comp.Suit, card2.Comp.Rank));
        handComp.FaceUp = card1.Comp.FaceUp;
        Dirty(hand, handComp);

        // For creating the hand of cards in the same slot
        _hands.TryDrop(user, card2);
        QueueDel(card1);
        QueueDel(card2);

        // Give to user in the same hand that was holding card
        _hands.TryPickup(user, hand, handId);

        UpdateHandName((hand, handComp));
        _popup.PopupEntity(Loc.GetString("rmc-playing-card-combine", ("count", 2)), hand, user);
    }

    protected override void AddCardToHand(Entity<PlayingCardHandComponent> hand, Entity<PlayingCardComponent> card, EntityUid user)
    {
        hand.Comp.Cards.Add(EncodeCard(card.Comp.Suit, card.Comp.Rank));
        Dirty(hand);

        QueueDel(card);

        UpdateHandName(hand);
        TryPopup(hand, Loc.GetString("rmc-playing-card-add-to-hand", ("count", hand.Comp.Cards.Count)), user);
    }

    protected override void MergeHands(Entity<PlayingCardHandComponent> hand1, Entity<PlayingCardHandComponent> hand2, EntityUid user)
    {
        // Add all cards from hand2 to hand1
        hand1.Comp.Cards.AddRange(hand2.Comp.Cards);
        Dirty(hand1);

        QueueDel(hand2);

        UpdateHandName(hand1);
        TryPopup(hand1, Loc.GetString("rmc-playing-card-merge-hands", ("count", hand1.Comp.Cards.Count)), user);
    }

    protected override void DrawFromHand(Entity<PlayingCardHandComponent> hand, EntityUid user)
    {
        DrawSpecificCard(hand, user, hand.Comp.Cards.Count - 1);
    }

    protected override void DrawSpecificCard(Entity<PlayingCardHandComponent> hand, EntityUid user, int index)
    {
        if (hand.Comp.Cards.Count == 0)
        {
            _popup.PopupEntity(Loc.GetString("rmc-playing-card-hand-empty"), hand, user);
            return;
        }

        if (index < 0 || index >= hand.Comp.Cards.Count)
            return;

        // Take the specified card
        var encoded = hand.Comp.Cards[index];
        hand.Comp.Cards.RemoveAt(index);
        Dirty(hand);

        var (suit, rank) = DecodeCard(encoded);

        // Spawn the card
        var coords = _transform.GetMapCoordinates(hand);
        var card = Spawn("RMCPlayingCard", coords);

        if (TryComp<PlayingCardComponent>(card, out var cardComp))
        {
            cardComp.Suit = suit;
            cardComp.Rank = rank;
            cardComp.FaceUp = hand.Comp.FaceUp;
            Dirty(card, cardComp);
        }

        UpdateCardAppearance(card, suit, rank, hand.Comp.FaceUp);

        // Give to user
        _hands.TryPickupAnyHand(user, card);

        // Only show value in popup if hand was face up
        if (hand.Comp.FaceUp)
        {
            var suitName = GetSuitDisplayName(suit);
            var rankName = GetRankDisplayName(rank);
            _popup.PopupEntity(Loc.GetString("rmc-playing-card-draw", ("rank", rankName), ("suit", suitName)), hand, user);
        }
        else
        {
            _popup.PopupEntity(Loc.GetString("rmc-playing-card-draw-hidden"), hand, user);
        }

        // If the hand is empty, delete it
        if (hand.Comp.Cards.Count == 0)
        {
            QueueDel(hand);
        }
        else
        {
            UpdateHandName(hand);
        }
    }

    private void UpdateCardAppearance(EntityUid card, CardSuit suit, CardRank rank, bool faceUp)
    {

    }

    private void UpdateHandName(Entity<PlayingCardHandComponent> hand)
    {
        var name = hand.Comp.Cards.Count > StackThreshold
            ? Loc.GetString("rmc-playing-card-stack-name")
            : Loc.GetString("rmc-playing-card-hand-name");
        _meta.SetEntityName(hand, name);
    }

    private void TryPopup(Entity<PlayingCardHandComponent> hand, string message, EntityUid user)
    {
        var curTime = _timing.CurTime;
        if (curTime < hand.Comp.LastPopupTime + TimeSpan.FromSeconds(hand.Comp.PopupCooldown))
            return;

        hand.Comp.LastPopupTime = curTime;
        _popup.PopupEntity(message, hand, user);
    }

    protected override void AddCardToDeck(Entity<PlayingCardDeckComponent> deck, Entity<PlayingCardComponent> card, EntityUid user)
    {
        if (deck.Comp.CardsRemaining >= deck.Comp.MaxCards)
        {
            _popup.PopupEntity(Loc.GetString("rmc-playing-card-deck-full"), deck, user);
            QueueDel(card);
            return;
        }

        deck.Comp.CardOrder.Add(EncodeCard(card.Comp.Suit, card.Comp.Rank));
        deck.Comp.CardsRemaining = deck.Comp.CardOrder.Count;
        Dirty(deck);

        QueueDel(card);

        _popup.PopupEntity(Loc.GetString("rmc-playing-card-added-to-deck"), deck, user);
        _audio.PlayPvs(deck.Comp.DrawSound, deck);
    }

    protected override void AddHandToDeck(Entity<PlayingCardDeckComponent> deck, Entity<PlayingCardHandComponent> hand, EntityUid user)
    {
        var added = 0;
        foreach (var encoded in hand.Comp.Cards)
        {
            if (deck.Comp.CardsRemaining >= deck.Comp.MaxCards)
                break;

            deck.Comp.CardOrder.Add(encoded);
            deck.Comp.CardsRemaining = deck.Comp.CardOrder.Count;
            added++;
        }

        Dirty(deck);
        QueueDel(hand);

        if (added > 0)
        {
            _popup.PopupEntity(Loc.GetString("rmc-playing-card-added-cards-to-deck", ("count", added)), deck, user);
            _audio.PlayPvs(deck.Comp.DrawSound, deck);
        }
    }
}
