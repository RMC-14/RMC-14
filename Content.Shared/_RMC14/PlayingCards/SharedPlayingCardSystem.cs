using Content.Shared.Examine;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.PlayingCards;

public abstract class SharedPlayingCardSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();

        // Card events
        SubscribeLocalEvent<PlayingCardComponent, UseInHandEvent>(OnCardUseInHand);
        SubscribeLocalEvent<PlayingCardComponent, ExaminedEvent>(OnCardExamined);
        SubscribeLocalEvent<PlayingCardComponent, GetVerbsEvent<AlternativeVerb>>(OnCardGetAltVerbs);
        SubscribeLocalEvent<PlayingCardComponent, InteractUsingEvent>(OnCardInteractUsing);

        // Deck events
        SubscribeLocalEvent<PlayingCardDeckComponent, UseInHandEvent>(OnDeckUseInHand);
        SubscribeLocalEvent<PlayingCardDeckComponent, ActivateInWorldEvent>(OnDeckActivateInWorld);
        SubscribeLocalEvent<PlayingCardDeckComponent, ExaminedEvent>(OnDeckExamined);
        SubscribeLocalEvent<PlayingCardDeckComponent, GetVerbsEvent<AlternativeVerb>>(OnDeckGetAltVerbs);
        SubscribeLocalEvent<PlayingCardDeckComponent, MapInitEvent>(OnDeckMapInit);

        // Hand of cards events
        SubscribeLocalEvent<PlayingCardHandComponent, UseInHandEvent>(OnHandUseInHand);
        SubscribeLocalEvent<PlayingCardHandComponent, ActivateInWorldEvent>(OnHandActivateInWorld);
        SubscribeLocalEvent<PlayingCardHandComponent, ExaminedEvent>(OnHandExamined);
        SubscribeLocalEvent<PlayingCardHandComponent, GetVerbsEvent<AlternativeVerb>>(OnHandGetAltVerbs);
        SubscribeLocalEvent<PlayingCardHandComponent, InteractUsingEvent>(OnHandInteractUsing);

        // BUI events
        Subs.BuiEvents<PlayingCardHandComponent>(PlayingCardHandUi.Key,
            subs =>
            {
                subs.Event<PlayingCardHandBuiMsg>(OnHandBuiMsg);
            });
    }

    private void OnHandBuiMsg(Entity<PlayingCardHandComponent> ent, ref PlayingCardHandBuiMsg args)
    {
        if (args.Actor is not { Valid: true } user)
            return;

        DrawSpecificCard(ent, user, args.CardIndex);
    }

    #region Card Events

    private void OnCardUseInHand(Entity<PlayingCardComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        // Flip the card
        FlipCard(ent, args.User);
        args.Handled = true;
    }

    private void OnCardExamined(Entity<PlayingCardComponent> ent, ref ExaminedEvent args)
    {
        if (!ent.Comp.FaceUp)
        {
            args.PushMarkup(Loc.GetString("rmc-playing-card-examine-face-down"));
            return;
        }

        var suit = GetSuitDisplayName(ent.Comp.Suit);
        var rank = GetRankDisplayName(ent.Comp.Rank);
        args.PushMarkup(Loc.GetString("rmc-playing-card-examine", ("rank", rank), ("suit", suit)));
    }

    private void OnCardGetAltVerbs(Entity<PlayingCardComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        var user = args.User;

        args.Verbs.Add(new AlternativeVerb
        {
            Text = Loc.GetString("rmc-playing-card-verb-flip"),
            Act = () => FlipCard(ent, user),
            Priority = 1
        });
    }

    private void OnCardInteractUsing(Entity<PlayingCardComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        // Combine cards into a hand
        if (TryComp<PlayingCardComponent>(args.Used, out var otherCard))
        {
            if (_net.IsServer)
                CombineCards(ent, (args.Used, otherCard), args.User);
            args.Handled = true;
            return;
        }

        // Add card to existing hand
        if (TryComp<PlayingCardHandComponent>(args.Used, out var hand))
        {
            if (_net.IsServer)
                AddCardToHand((args.Used, hand), ent, args.User);
            args.Handled = true;
        }
    }

    #endregion

    #region Deck Events

    private void OnDeckMapInit(Entity<PlayingCardDeckComponent> ent, ref MapInitEvent args)
    {
        ShuffleDeck(ent);
    }

    private void OnDeckUseInHand(Entity<PlayingCardDeckComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        if (_net.IsServer)
            DrawCard(ent, args.User);
        args.Handled = true;
    }

    private void OnDeckActivateInWorld(Entity<PlayingCardDeckComponent> ent, ref ActivateInWorldEvent args)
    {
        if (args.Handled)
            return;

        if (_net.IsServer)
            DrawCard(ent, args.User);
        args.Handled = true;
    }

    private void OnDeckExamined(Entity<PlayingCardDeckComponent> ent, ref ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("rmc-playing-card-deck-examine", ("count", ent.Comp.CardsRemaining)));
    }

    private void OnDeckGetAltVerbs(Entity<PlayingCardDeckComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        var user = args.User;

        args.Verbs.Add(new AlternativeVerb
        {
            Text = Loc.GetString("rmc-playing-card-verb-shuffle"),
            Act = () =>
            {
                ShuffleDeck(ent);
                _popup.PopupPredicted(Loc.GetString("rmc-playing-card-deck-shuffle", ("deck", ent.Owner)), ent, user);
                _audio.PlayPredicted(ent.Comp.ShuffleSound, ent, user);
            },
            Priority = 2
        });

        args.Verbs.Add(new AlternativeVerb
        {
            Text = Loc.GetString("rmc-playing-card-verb-draw"),
            Act = () =>
            {
                if (_net.IsServer)
                    DrawCard(ent, user);
            },
            Priority = 1
        });
    }

    #endregion

    #region Hand Events

    private void OnHandUseInHand(Entity<PlayingCardHandComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        // If face up, open radial menu to select card
        if (ent.Comp.FaceUp && ent.Comp.Cards.Count > 0)
        {
            _ui.OpenUi(ent.Owner, PlayingCardHandUi.Key, args.User);
            args.Handled = true;
            return;
        }

        // Draw a card from the hand (face down = draw from top)
        if (_net.IsServer)
            DrawFromHand(ent, args.User);
        args.Handled = true;
    }

    private void OnHandActivateInWorld(Entity<PlayingCardHandComponent> ent, ref ActivateInWorldEvent args)
    {
        if (args.Handled)
            return;

        // If face up; pressing E opens radial menu to select card
        if (ent.Comp.FaceUp && ent.Comp.Cards.Count > 0)
        {
            _ui.OpenUi(ent.Owner, PlayingCardHandUi.Key, args.User);
            args.Handled = true;
            return;
        }

        // Pressing E draws a card from the hand (face down = draw from top)
        if (_net.IsServer)
            DrawFromHand(ent, args.User);
        args.Handled = true;
    }

    private void OnHandExamined(Entity<PlayingCardHandComponent> ent, ref ExaminedEvent args)
    {
        var count = ent.Comp.Cards.Count;
        if (!ent.Comp.FaceUp)
        {
            args.PushMarkup(Loc.GetString("rmc-playing-card-hand-examine-hidden", ("count", count)));
            return;
        }

        // If in someone's hands and not the examiner's, hide the cards
        if (_container.TryGetContainingContainer(ent.Owner, out var container) &&
            container.Owner != args.Examiner &&
            _hands.IsHolding(container.Owner, ent.Owner))
        {
            args.PushMarkup(Loc.GetString("rmc-playing-card-hand-examine-hidden", ("count", count)));
            return;
        }

        args.PushMarkup(Loc.GetString("rmc-playing-card-hand-examine", ("count", count)));
        foreach (var encoded in ent.Comp.Cards)
        {
            var (suit, rank) = DecodeCard(encoded);
            var suitName = GetSuitDisplayName(suit);
            var rankName = GetRankDisplayName(rank);
            args.PushMarkup(Loc.GetString("rmc-playing-card-hand-card", ("rank", rankName), ("suit", suitName)));
        }
    }

    private void OnHandGetAltVerbs(Entity<PlayingCardHandComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        var user = args.User;

        args.Verbs.Add(new AlternativeVerb
        {
            Text = Loc.GetString("rmc-playing-card-verb-flip"),
            Act = () => FlipHand(ent, user),
            Priority = 2
        });

        if (ent.Comp.Cards.Count > 0)
        {
            // If face up, open radial menu; if face down, draw from top
            if (ent.Comp.FaceUp)
            {
                args.Verbs.Add(new AlternativeVerb
                {
                    Text = Loc.GetString("rmc-playing-card-verb-pick"),
                    Act = () => _ui.OpenUi(ent.Owner, PlayingCardHandUi.Key, user),
                    Priority = 1
                });
            }
            else
            {
                // Face down - can only draw from top
                args.Verbs.Add(new AlternativeVerb
                {
                    Text = Loc.GetString("rmc-playing-card-verb-draw"),
                    Act = () =>
                    {
                        if (_net.IsServer)
                            DrawFromHand(ent, user);
                    },
                    Priority = 1
                });
            }
        }
    }

    private void OnHandInteractUsing(Entity<PlayingCardHandComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        // Add card to hand
        if (TryComp<PlayingCardComponent>(args.Used, out var card))
        {
            if (_net.IsServer)
                AddCardToHand(ent, (args.Used, card), args.User);
            args.Handled = true;
            return;
        }

        // Merge hands
        if (TryComp<PlayingCardHandComponent>(args.Used, out var otherHand))
        {
            if (_net.IsServer)
                MergeHands(ent, (args.Used, otherHand), args.User);
            args.Handled = true;
        }
    }

    #endregion

    #region Card Logic

    public virtual void FlipCard(Entity<PlayingCardComponent> card, EntityUid user)
    {
        card.Comp.FaceUp = !card.Comp.FaceUp;
        Dirty(card);

        var direction = card.Comp.FaceUp ? "up" : "down";
        _popup.PopupPredicted(Loc.GetString("rmc-playing-card-flip", ("direction", direction)), card, user);
    }

    public virtual void FlipHand(Entity<PlayingCardHandComponent> hand, EntityUid user)
    {
        hand.Comp.FaceUp = !hand.Comp.FaceUp;
        Dirty(hand);

        var direction = hand.Comp.FaceUp ? "up" : "down";
        _popup.PopupPredicted(Loc.GetString("rmc-playing-card-hand-flip", ("direction", direction)), hand, user);
    }

    protected virtual void CombineCards(Entity<PlayingCardComponent> card1, Entity<PlayingCardComponent> card2, EntityUid user)
    {
    }

    protected virtual void AddCardToHand(Entity<PlayingCardHandComponent> hand, Entity<PlayingCardComponent> card, EntityUid user)
    {
    }

    protected virtual void MergeHands(Entity<PlayingCardHandComponent> hand1, Entity<PlayingCardHandComponent> hand2, EntityUid user)
    {
    }

    protected virtual void DrawFromHand(Entity<PlayingCardHandComponent> hand, EntityUid user)
    {
    }

    protected virtual void DrawSpecificCard(Entity<PlayingCardHandComponent> hand, EntityUid user, int index)
    {
    }

    #endregion

    #region Deck Logic

    public void ShuffleDeck(Entity<PlayingCardDeckComponent> deck)
    {
        deck.Comp.CardOrder.Clear();

        // Create all 52 cards
        foreach (CardSuit suit in Enum.GetValues<CardSuit>())
        {
            foreach (CardRank rank in Enum.GetValues<CardRank>())
            {
                deck.Comp.CardOrder.Add(EncodeCard(suit, rank));
            }
        }

        // Fisher-Yates shuffle
        for (var i = deck.Comp.CardOrder.Count - 1; i > 0; i--)
        {
            var j = _random.Next(i + 1);
            (deck.Comp.CardOrder[i], deck.Comp.CardOrder[j]) = (deck.Comp.CardOrder[j], deck.Comp.CardOrder[i]);
        }

        deck.Comp.CardsRemaining = deck.Comp.CardOrder.Count;
        Dirty(deck);
    }

    protected virtual void DrawCard(Entity<PlayingCardDeckComponent> deck, EntityUid user)
    {
    }

    #endregion

    #region Helpers

    public static int EncodeCard(CardSuit suit, CardRank rank)
    {
        return ((int)suit << 8) | (int)rank;
    }

    public static (CardSuit Suit, CardRank Rank) DecodeCard(int encoded)
    {
        var suit = (CardSuit)(encoded >> 8);
        var rank = (CardRank)(encoded & 0xFF);
        return (suit, rank);
    }

    public string GetSuitDisplayName(CardSuit suit)
    {
        return suit switch
        {
            CardSuit.Spades => Loc.GetString("rmc-playing-card-suit-spades"),
            CardSuit.Hearts => Loc.GetString("rmc-playing-card-suit-hearts"),
            CardSuit.Diamonds => Loc.GetString("rmc-playing-card-suit-diamonds"),
            CardSuit.Clubs => Loc.GetString("rmc-playing-card-suit-clubs"),
            _ => suit.ToString()
        };
    }

    public string GetRankDisplayName(CardRank rank)
    {
        return rank switch
        {
            CardRank.Ace => Loc.GetString("rmc-playing-card-rank-ace"),
            CardRank.Two => "2",
            CardRank.Three => "3",
            CardRank.Four => "4",
            CardRank.Five => "5",
            CardRank.Six => "6",
            CardRank.Seven => "7",
            CardRank.Eight => "8",
            CardRank.Nine => "9",
            CardRank.Ten => "10",
            CardRank.Jack => Loc.GetString("rmc-playing-card-rank-jack"),
            CardRank.Queen => Loc.GetString("rmc-playing-card-rank-queen"),
            CardRank.King => Loc.GetString("rmc-playing-card-rank-king"),
            _ => ((int)rank).ToString()
        };
    }

    public static string GetCardStateName(CardSuit suit, CardRank rank)
    {
        var suitName = suit.ToString().ToLowerInvariant();
        var rankName = rank switch
        {
            CardRank.Ace => "ace",
            CardRank.Two => "two",
            CardRank.Three => "three",
            CardRank.Four => "four",
            CardRank.Five => "five",
            CardRank.Six => "six",
            CardRank.Seven => "seven",
            CardRank.Eight => "eight",
            CardRank.Nine => "nine",
            CardRank.Ten => "ten",
            CardRank.Jack => "jack",
            CardRank.Queen => "queen",
            CardRank.King => "king",
            _ => ((int)rank).ToString()
        };
        return $"{suitName}_{rankName}";
    }

    #endregion
}
