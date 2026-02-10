using Content.Server.Hands.Systems;
using Content.Shared._RMC14.PlayingCards;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
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

    private EntityUid SpawnCard(string prototype, EntityUid source, CardSuit suit, CardRank rank, bool faceUp)
    {
        var card = Spawn(prototype, _transform.GetMapCoordinates(source));
        if (TryComp<PlayingCardComponent>(card, out var comp))
        {
            comp.Suit = suit;
            comp.Rank = rank;
            comp.FaceUp = faceUp;
            Dirty(card, comp);
        }
        return card;
    }

    protected override void DrawCard(Entity<PlayingCardDeckComponent> deck, EntityUid user)
    {
        if (deck.Comp.CardOrder.Count == 0)
        {
            _popup.PopupEntity(Loc.GetString("rmc-playing-card-deck-empty"), deck, user);
            return;
        }

        var cardIndex = deck.Comp.CardOrder.Count - 1;
        var (suit, rank) = DecodeCard(deck.Comp.CardOrder[cardIndex]);
        deck.Comp.CardOrder.RemoveAt(cardIndex);
        deck.Comp.CardsRemaining = deck.Comp.CardOrder.Count;
        Dirty(deck);

        var card = SpawnCard(deck.Comp.CardPrototype, deck, suit, rank, false);
        _hands.TryPickupAnyHand(user, card);

        _popup.PopupEntity(Loc.GetString("rmc-playing-card-draw-deck"), deck, user);
        _audio.PlayPvs(deck.Comp.DrawSound, deck);
    }

    protected override void CombineCards(Entity<PlayingCardComponent> card1, Entity<PlayingCardComponent> card2, EntityUid user)
    {
        _hands.IsHolding(user, card2, out var handId);

        var hand = Spawn("RMCPlayingCardHand", _transform.GetMapCoordinates(card1));
        if (!TryComp<PlayingCardHandComponent>(hand, out var handComp))
        {
            QueueDel(hand);
            return;
        }

        handComp.Cards.Add(EncodeCard(card1.Comp.Suit, card1.Comp.Rank));
        handComp.Cards.Add(EncodeCard(card2.Comp.Suit, card2.Comp.Rank));
        handComp.FaceUp = card1.Comp.FaceUp;
        Dirty(hand, handComp);

        _hands.TryDrop(user, card2);
        QueueDel(card1);
        QueueDel(card2);

        _hands.TryPickup(user, hand, handId);
        UpdateHandName((hand, handComp));
        TryPopup((hand, handComp), Loc.GetString("rmc-playing-card-add-to-hand", ("count", handComp.Cards.Count)), user);
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
        hand1.Comp.Cards.AddRange(hand2.Comp.Cards);
        Dirty(hand1);
        QueueDel(hand2);
        UpdateHandName(hand1);
        TryPopup(hand1, Loc.GetString("rmc-playing-card-merge-hands", ("count", hand1.Comp.Cards.Count)), user);
    }

    protected override void DrawFromHand(Entity<PlayingCardHandComponent> hand, EntityUid user)
        => DrawSpecificCard(hand, user, hand.Comp.Cards.Count - 1);

    protected override void DrawSpecificCard(Entity<PlayingCardHandComponent> hand, EntityUid user, int index)
    {
        if (hand.Comp.Cards.Count == 0)
        {
            _popup.PopupEntity(Loc.GetString("rmc-playing-card-hand-empty"), hand, user);
            return;
        }

        if (index < 0 || index >= hand.Comp.Cards.Count)
            return;

        var (suit, rank) = DecodeCard(hand.Comp.Cards[index]);
        hand.Comp.Cards.RemoveAt(index);
        Dirty(hand);

        var card = SpawnCard("RMCPlayingCard", hand, suit, rank, hand.Comp.FaceUp);
        _hands.TryPickupAnyHand(user, card);

        if (hand.Comp.FaceUp)
            _popup.PopupEntity(Loc.GetString("rmc-playing-card-draw", ("rank", GetRankDisplayName(rank)), ("suit", GetSuitDisplayName(suit))), hand, user);
        else
            _popup.PopupEntity(Loc.GetString("rmc-playing-card-draw-hidden"), hand, user);

        if (hand.Comp.Cards.Count == 1)
        {
            var (lastSuit, lastRank) = DecodeCard(hand.Comp.Cards[0]);
            _hands.IsHolding(user, hand, out var heldHandSlot);
            var lastCard = SpawnCard("RMCPlayingCard", hand, lastSuit, lastRank, hand.Comp.FaceUp);

            _hands.TryDrop(user, hand);
            QueueDel(hand);
            _hands.TryPickup(user, lastCard, heldHandSlot);
        }
        else if (hand.Comp.Cards.Count == 0)
            QueueDel(hand);
        else
            UpdateHandName(hand);
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
        if (deck.Comp.CardsRemaining >= deck.Comp.MaxCards)
        {
            _popup.PopupEntity(Loc.GetString("rmc-playing-card-deck-full"), deck, user);
            return;
        }

        var added = 0;
        var cardsToRemove = new List<int>();
        for (var i = 0; i < hand.Comp.Cards.Count; i++)
        {
            if (deck.Comp.CardsRemaining >= deck.Comp.MaxCards)
                break;

            deck.Comp.CardOrder.Add(hand.Comp.Cards[i]);
            deck.Comp.CardsRemaining = deck.Comp.CardOrder.Count;
            cardsToRemove.Add(i);
            added++;
        }

        // Remove added cards from hand in reverse order
        for (var i = cardsToRemove.Count - 1; i >= 0; i--)
        {
            hand.Comp.Cards.RemoveAt(cardsToRemove[i]);
        }

        Dirty(deck);

        if (hand.Comp.Cards.Count == 0)
            QueueDel(hand);
        else
            Dirty(hand);

        if (added > 0)
        {
            _popup.PopupEntity(Loc.GetString("rmc-playing-card-added-cards-to-deck", ("count", added)), deck, user);
            _audio.PlayPvs(deck.Comp.DrawSound, deck);
        }
    }
}
