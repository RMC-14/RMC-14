using Content.Shared._RMC14.PlayingCards;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server._RMC14.PlayingCards;

public sealed class PlayingCardSystem : SharedPlayingCardSystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private static readonly EntProtoId CardProto = "RMCPlayingCard";
    private static readonly EntProtoId CardHandProto = "RMCPlayingCardHand";

    private EntityUid SpawnCard(EntProtoId prototype, EntityUid source, CardSuit suit, CardRank rank, bool faceUp)
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
            Popup.PopupEntity(Loc.GetString("rmc-playing-card-deck-empty"), deck, user);
            return;
        }

        var cardIndex = deck.Comp.CardOrder.Count - 1;
        var (suit, rank) = DecodeCard(deck.Comp.CardOrder[cardIndex]);
        deck.Comp.CardOrder.RemoveAt(cardIndex);
        Dirty(deck);

        var card = SpawnCard(deck.Comp.CardPrototype, deck, suit, rank, false);
        Hands.TryPickupAnyHand(user, card);

        Popup.PopupEntity(Loc.GetString("rmc-playing-card-draw-deck"), deck, user);
        Audio.PlayPvs(deck.Comp.DrawSound, deck);
    }

    protected override void DrawMultiple(Entity<PlayingCardDeckComponent> deck, EntityUid user, int count)
    {
        if (deck.Comp.CardOrder.Count == 0)
        {
            Popup.PopupEntity(Loc.GetString("rmc-playing-card-deck-empty"), deck, user);
            return;
        }

        var actualCount = Math.Min(count, deck.Comp.CardOrder.Count);
        if (actualCount <= 0)
            return;

        var hand = Spawn(CardHandProto, _transform.GetMapCoordinates(deck));
        if (!TryComp<PlayingCardHandComponent>(hand, out var handComp))
        {
            QueueDel(hand);
            return;
        }

        for (var i = 0; i < actualCount; i++)
        {
            var cardIndex = deck.Comp.CardOrder.Count - 1;
            handComp.Cards.Add(deck.Comp.CardOrder[cardIndex]);
            deck.Comp.CardOrder.RemoveAt(cardIndex);
        }

        handComp.FaceUp = false;
        Dirty(deck);
        Dirty(hand, handComp);

        Hands.TryPickupAnyHand(user, hand);
        UpdateHandName((hand, handComp));

        Popup.PopupEntity(Loc.GetString("rmc-playing-card-draw-multiple", ("count", actualCount)), deck, user);
        Audio.PlayPvs(deck.Comp.DrawSound, deck);
    }

    protected override void CombineCards(Entity<PlayingCardComponent> card1, Entity<PlayingCardComponent> card2, EntityUid user)
    {
        Hands.IsHolding(user, card2, out var handId);

        var hand = Spawn(CardHandProto, _transform.GetMapCoordinates(card1));
        if (!TryComp<PlayingCardHandComponent>(hand, out var handComp))
        {
            QueueDel(hand);
            return;
        }

        handComp.Cards.Add(EncodeCard(card1.Comp.Suit, card1.Comp.Rank));
        handComp.Cards.Add(EncodeCard(card2.Comp.Suit, card2.Comp.Rank));
        handComp.FaceUp = card1.Comp.FaceUp;
        Dirty(hand, handComp);

        Hands.TryDrop(user, card2);
        QueueDel(card1);
        QueueDel(card2);

        Hands.TryPickup(user, hand, handId);
        UpdateHandName((hand, handComp));
        TryPopup((hand, handComp), Loc.GetString("rmc-playing-card-add-to-hand", ("count", handComp.Cards.Count)), user);
    }

    protected override void DrawFromHand(Entity<PlayingCardHandComponent> hand, EntityUid user)
    {
        DrawSpecificCard(hand, user, hand.Comp.Cards.Count - 1);
    }

    protected override void DrawSpecificCard(Entity<PlayingCardHandComponent> hand, EntityUid user, int index)
    {
        if (hand.Comp.Cards.Count == 0)
        {
            Popup.PopupEntity(Loc.GetString("rmc-playing-card-hand-empty"), hand, user);
            return;
        }

        if (index < 0 || index >= hand.Comp.Cards.Count)
            return;

        var (suit, rank) = DecodeCard(hand.Comp.Cards[index]);
        hand.Comp.Cards.RemoveAt(index);
        Dirty(hand);

        var card = SpawnCard(CardProto, hand, suit, rank, hand.Comp.FaceUp);
        Hands.TryPickupAnyHand(user, card);

        if (hand.Comp.FaceUp)
        {
            var rankName = FormattedMessage.RemoveMarkupPermissive(GetRankDisplayName(rank));
            var suitName = FormattedMessage.RemoveMarkupPermissive(GetSuitDisplayName(suit));
            Popup.PopupEntity(Loc.GetString("rmc-playing-card-draw", ("rank", rankName), ("suit", suitName)), hand, user);
        }
        else
            Popup.PopupEntity(Loc.GetString("rmc-playing-card-draw-hidden"), hand, user);

        if (hand.Comp.Cards.Count == 1)
        {
            var (lastSuit, lastRank) = DecodeCard(hand.Comp.Cards[0]);
            Hands.IsHolding(user, hand, out var heldHandSlot);
            var lastCard = SpawnCard(CardProto, hand, lastSuit, lastRank, hand.Comp.FaceUp);

            Hands.TryDrop(user, hand);
            QueueDel(hand);
            Hands.TryPickup(user, lastCard, heldHandSlot);
        }
        else if (hand.Comp.Cards.Count == 0)
            QueueDel(hand);
        else
            UpdateHandName(hand);
    }

}
