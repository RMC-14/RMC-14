using Content.Shared._RMC14.Hands;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.PlayingCards;

public abstract class SharedPlayingCardSystem : EntitySystem
{
    [Dependency] protected readonly SharedAudioSystem Audio = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly ExamineSystemShared _examine = default!;
    [Dependency] protected readonly SharedHandsSystem Hands = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly MetaDataSystem _meta = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] protected readonly SharedPopupSystem Popup = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] protected readonly SharedUserInterfaceSystem Ui = default!;

    private readonly HashSet<Entity<PlayingCardComponent>> _cardLookup = new();
    private readonly HashSet<Entity<PlayingCardHandComponent>> _handLookup = new();

    private EntityQuery<PlayingCardComponent> _cardQuery;
    private EntityQuery<PlayingCardDeckComponent> _deckQuery;
    private EntityQuery<PlayingCardHandComponent> _handQuery;

    private static readonly VerbCategory DrawCategory = new("rmc-playing-card-verb-category-draw", null);

    private const float AreaPickupRadius = 1f;
    private const float AreaPickupDelayPerCard = 0.1f;
    private const int StackThreshold = 5;
    private const int DrawFiveCount = 5;
    private const int AceSortValue = 14;

    public override void Initialize()
    {
        base.Initialize();

        _cardQuery = GetEntityQuery<PlayingCardComponent>();
        _deckQuery = GetEntityQuery<PlayingCardDeckComponent>();
        _handQuery = GetEntityQuery<PlayingCardHandComponent>();

        // Card events
        SubscribeLocalEvent<PlayingCardComponent, UseInHandEvent>(OnCardUseInHand);
        SubscribeLocalEvent<PlayingCardComponent, ExaminedEvent>(OnCardExamined);
        SubscribeLocalEvent<PlayingCardComponent, GetVerbsEvent<AlternativeVerb>>(OnCardGetAltVerbs);
        SubscribeLocalEvent<PlayingCardComponent, InteractUsingEvent>(OnCardInteractUsing);

        // Deck events
        SubscribeLocalEvent<PlayingCardDeckComponent, UseInHandEvent>(OnDeckUseInHand);
        SubscribeLocalEvent<PlayingCardDeckComponent, ActivateInWorldEvent>(OnDeckActivateInWorld);
        SubscribeLocalEvent<PlayingCardDeckComponent, AfterInteractEvent>(OnDeckAfterInteract);
        SubscribeLocalEvent<PlayingCardDeckComponent, ExaminedEvent>(OnDeckExamined);
        SubscribeLocalEvent<PlayingCardDeckComponent, GetVerbsEvent<ExamineVerb>>(OnDeckGetExamineVerbs);
        SubscribeLocalEvent<PlayingCardDeckComponent, GetVerbsEvent<AlternativeVerb>>(OnDeckGetAltVerbs);
        SubscribeLocalEvent<PlayingCardDeckComponent, MapInitEvent>(OnDeckMapInit);
        SubscribeLocalEvent<PlayingCardDeckComponent, InteractUsingEvent>(OnDeckInteractUsing);
        SubscribeLocalEvent<PlayingCardDeckComponent, PlayingCardDeckPickupDoAfterEvent>(OnDeckPickupDoAfter);
        SubscribeLocalEvent<PlayingCardDeckComponent, RMCStorageEjectHandItemEvent>(OnDeckEjectHand);

        // Hand of cards events
        SubscribeLocalEvent<PlayingCardHandComponent, UseInHandEvent>(OnHandUseInHand);
        SubscribeLocalEvent<PlayingCardHandComponent, ActivateInWorldEvent>(OnHandActivateInWorld);
        SubscribeLocalEvent<PlayingCardHandComponent, ExaminedEvent>(OnHandExamined);
        SubscribeLocalEvent<PlayingCardHandComponent, GetVerbsEvent<ExamineVerb>>(OnHandGetExamineVerbs);
        SubscribeLocalEvent<PlayingCardHandComponent, GetVerbsEvent<AlternativeVerb>>(OnHandGetAltVerbs);
        SubscribeLocalEvent<PlayingCardHandComponent, InteractUsingEvent>(OnHandInteractUsing);
        SubscribeLocalEvent<PlayingCardHandComponent, RMCStorageEjectHandItemEvent>(OnHandEjectHand);

        // BUI events
        Subs.BuiEvents<PlayingCardHandComponent>(PlayingCardHandUi.Key,
            subs =>
            {
                subs.Event<PlayingCardHandBuiMsg>(OnHandBuiMsg);
            });
    }

    private void OnHandBuiMsg(Entity<PlayingCardHandComponent> ent, ref PlayingCardHandBuiMsg args)
    {
        DrawSpecificCard(ent, args.Actor, args.CardIndex);
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
        if (_cardQuery.TryComp(args.Used, out var otherCard))
        {
            if (_net.IsServer)
                CombineCards(ent, (args.Used, otherCard), args.User);
            args.Handled = true;
            return;
        }

        // Add card to existing hand
        if (_handQuery.TryComp(args.Used, out var hand))
        {
            AddCardToHand((args.Used, hand), ent, args.User);
            args.Handled = true;
            return;
        }

        // Add card to deck (deck in active hand clicked on this card)
        if (_deckQuery.TryComp(args.Used, out var deck))
        {
            AddCardToDeck((args.Used, deck), ent, args.User);
            args.Handled = true;
        }
    }

    #endregion

    #region Deck Events

    private void OnDeckMapInit(Entity<PlayingCardDeckComponent> ent, ref MapInitEvent args)
    {
        InitializeDeck(ent);
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
        args.PushMarkup(Loc.GetString("rmc-playing-card-deck-examine", ("count", ent.Comp.CardOrder.Count)));
    }

    private void OnDeckGetExamineVerbs(Entity<PlayingCardDeckComponent> ent, ref GetVerbsEvent<ExamineVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        var msg = new FormattedMessage();
        msg.AddMarkupOrThrow(Loc.GetString("rmc-playing-card-deck-examine-shuffle"));
        msg.PushNewline();
        msg.AddMarkupOrThrow(Loc.GetString("rmc-playing-card-deck-examine-draw"));
        msg.PushNewline();
        msg.AddMarkupOrThrow(Loc.GetString("rmc-playing-card-deck-examine-pickup"));

        _examine.AddDetailedExamineVerb(
            args,
            ent.Comp,
            msg,
            Loc.GetString("rmc-playing-card-deck-examine-verb"),
            "/Textures/Interface/VerbIcons/examine.svg.192dpi.png",
            Loc.GetString("rmc-playing-card-deck-examine-verb-message"));
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
                if (_net.IsServer)
                    ShuffleDeck(ent);
                Popup.PopupPredicted(Loc.GetString("rmc-playing-card-deck-shuffle", ("deck", ent.Owner)), null, ent, user);
                Audio.PlayPredicted(ent.Comp.ShuffleSound, ent, user);
            },
            Priority = 2
        });

        var deckCount = ent.Comp.CardOrder.Count;

        args.Verbs.Add(new AlternativeVerb
        {
            Text = Loc.GetString("rmc-playing-card-verb-draw-5"),
            Category = DrawCategory,
            Act = () =>
            {
                if (_net.IsServer)
                    DrawMultiple(ent, user, DrawFiveCount);
            },
            Priority = 2
        });

        args.Verbs.Add(new AlternativeVerb
        {
            Text = Loc.GetString("rmc-playing-card-verb-draw-half"),
            Category = DrawCategory,
            Act = () =>
            {
                if (_net.IsServer)
                    DrawMultiple(ent, user, Math.Max(1, deckCount / 2));
            },
            Priority = 1
        });

        args.Verbs.Add(new AlternativeVerb
        {
            Text = Loc.GetString("rmc-playing-card-verb-draw-all"),
            Category = DrawCategory,
            Act = () =>
            {
                if (_net.IsServer)
                    DrawMultiple(ent, user, deckCount);
            },
            Priority = 0
        });
    }

    private void OnDeckInteractUsing(Entity<PlayingCardDeckComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        // Add single card to deck
        if (_cardQuery.TryComp(args.Used, out var card))
        {
            AddCardToDeck(ent, (args.Used, card), args.User);
            args.Handled = true;
            return;
        }

        // Add hand of cards to deck
        if (_handQuery.TryComp(args.Used, out var hand))
        {
            AddHandToDeck(ent, (args.Used, hand), args.User);
            args.Handled = true;
        }
    }

    private void OnDeckAfterInteract(Entity<PlayingCardDeckComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach)
            return;

        if (args.Target != null && (HasComp<PlayingCardComponent>(args.Target) || HasComp<PlayingCardHandComponent>(args.Target)))
            return;

        // Find all cards and hands in range
        var cards = new List<EntityUid>();
        _cardLookup.Clear();
        _handLookup.Clear();
        _entityLookup.GetEntitiesInRange(args.ClickLocation, AreaPickupRadius, _cardLookup);
        _entityLookup.GetEntitiesInRange(args.ClickLocation, AreaPickupRadius, _handLookup);

        foreach (var card in _cardLookup)
        {
            if (!_interaction.InRangeUnobstructed(args.User, card.Owner))
                continue;

            if (_container.IsEntityInContainer(card.Owner))
                continue;

            cards.Add(card.Owner);
        }

        foreach (var hand in _handLookup)
        {
            if (!_interaction.InRangeUnobstructed(args.User, hand.Owner))
                continue;

            if (_container.IsEntityInContainer(hand.Owner))
                continue;

            cards.Add(hand.Owner);
        }

        if (cards.Count == 0)
            return;

        var delay = cards.Count * AreaPickupDelayPerCard;
        var doAfterArgs = new DoAfterArgs(EntityManager, args.User, delay, new PlayingCardDeckPickupDoAfterEvent(GetNetEntityList(cards)), ent, target: ent)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
            NeedHand = true,
        };

        _doAfter.TryStartDoAfter(doAfterArgs);
        args.Handled = true;
    }

    private void OnDeckPickupDoAfter(Entity<PlayingCardDeckComponent> ent, ref PlayingCardDeckPickupDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        args.Handled = true;

        if (_net.IsClient)
            return;

        if (ent.Comp.CardOrder.Count >= ent.Comp.MaxCards)
        {
            Popup.PopupEntity(Loc.GetString("rmc-playing-card-deck-full"), ent, args.User);
            return;
        }

        var added = 0;
        foreach (var netEntity in args.Entities)
        {
            var entity = GetEntity(netEntity);
            if (!Exists(entity))
                continue;

            if (ent.Comp.CardOrder.Count >= ent.Comp.MaxCards)
                break;

            if (_cardQuery.TryComp(entity, out var card))
            {
                ent.Comp.CardOrder.Add(EncodeCard(card.Suit, card.Rank));
                QueueDel(entity);
                added++;
            }
            else if (_handQuery.TryComp(entity, out var hand))
            {
                var handAdded = 0;
                foreach (var encodedCard in hand.Cards)
                {
                    if (ent.Comp.CardOrder.Count >= ent.Comp.MaxCards)
                        break;

                    ent.Comp.CardOrder.Add(encodedCard);
                    handAdded++;
                    added++;
                }

                hand.Cards.RemoveRange(0, handAdded);

                if (hand.Cards.Count == 0)
                    QueueDel(entity);
                else
                    Dirty(entity, hand);
            }
        }

        if (added > 0)
        {
            Dirty(ent);
            Popup.PopupEntity(Loc.GetString("rmc-playing-card-deck-pickup", ("count", added)), ent, args.User);
            Audio.PlayPvs(ent.Comp.DrawSound, ent);
        }
    }

    #endregion

    #region Hand Events

    private void OnHandUseInHand(Entity<PlayingCardHandComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;
        if (ent.Comp.FaceUp && ent.Comp.Cards.Count > 0)
            Ui.OpenUi(ent.Owner, PlayingCardHandUi.Key, args.User);
        else if (_net.IsServer)
            DrawFromHand(ent, args.User);
        args.Handled = true;
    }

    private void OnHandActivateInWorld(Entity<PlayingCardHandComponent> ent, ref ActivateInWorldEvent args)
    {
        if (args.Handled)
            return;
        if (ent.Comp.FaceUp && ent.Comp.Cards.Count > 0)
            Ui.OpenUi(ent.Owner, PlayingCardHandUi.Key, args.User);
        else if (_net.IsServer)
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
            Hands.IsHolding(container.Owner, ent.Owner))
        {
            args.PushMarkup(Loc.GetString("rmc-playing-card-hand-examine-hidden", ("count", count)));
            return;
        }

        args.PushMarkup(Loc.GetString("rmc-playing-card-hand-examine", ("count", count)));

        var suitOrder = new List<CardSuit>();
        var bySuit = new Dictionary<CardSuit, List<CardRank>>();
        foreach (var encoded in ent.Comp.Cards)
        {
            var (suit, rank) = DecodeCard(encoded);
            if (!bySuit.TryGetValue(suit, out var ranks))
            {
                ranks = new List<CardRank>();
                bySuit[suit] = ranks;
                suitOrder.Add(suit);
            }
            ranks.Add(rank);
        }

        foreach (var suit in suitOrder)
        {
            bySuit[suit].Sort((a, b) =>
            {
                var va = a == CardRank.Ace ? AceSortValue : (int)a;
                var vb = b == CardRank.Ace ? AceSortValue : (int)b;
                return va.CompareTo(vb);
            });
            var suitName = GetSuitDisplayName(suit);
            var rankList = string.Join(", ", bySuit[suit].ConvertAll(GetRankShortName));
            args.PushMarkup(Loc.GetString("rmc-playing-card-hand-suit-group", ("ranks", rankList), ("suit", suitName)));
        }
    }

    private void OnHandGetExamineVerbs(Entity<PlayingCardHandComponent> ent, ref GetVerbsEvent<ExamineVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        var msg = new FormattedMessage();
        msg.AddMarkupOrThrow(Loc.GetString("rmc-playing-card-hand-examine-face-down"));
        msg.PushNewline();
        msg.AddMarkupOrThrow(Loc.GetString("rmc-playing-card-hand-examine-face-up"));
        msg.PushNewline();
        msg.AddMarkupOrThrow(Loc.GetString("rmc-playing-card-hand-examine-flip"));

        _examine.AddDetailedExamineVerb(
            args,
            ent.Comp,
            msg,
            Loc.GetString("rmc-playing-card-hand-examine-verb"),
            "/Textures/Interface/VerbIcons/examine.svg.192dpi.png",
            Loc.GetString("rmc-playing-card-hand-examine-verb-message"));
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
            Priority = 3
        });

        args.Verbs.Add(new AlternativeVerb
        {
            Text = Loc.GetString("rmc-playing-card-verb-shuffle"),
            Act = () =>
            {
                if (_net.IsServer)
                    ShuffleHand(ent);
                Popup.PopupPredicted(Loc.GetString("rmc-playing-card-hand-shuffle", ("hand", ent.Owner)), null, ent, user);
                Audio.PlayPredicted(ent.Comp.ShuffleSound, ent, user);
            },
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
                    Act = () => Ui.OpenUi(ent.Owner, PlayingCardHandUi.Key, user),
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
        if (_cardQuery.TryComp(args.Used, out var card))
        {
            AddCardToHand(ent, (args.Used, card), args.User);
            args.Handled = true;
            return;
        }

        // Merge hands
        if (_handQuery.TryComp(args.Used, out var otherHand))
        {
            MergeHands(ent, (args.Used, otherHand), args.User);
            args.Handled = true;
            return;
        }

        // Add hand to deck
        if (_deckQuery.TryComp(args.Used, out var deck))
        {
            AddHandToDeck((args.Used, deck), ent, args.User);
            args.Handled = true;
        }
    }

    private void OnDeckEjectHand(Entity<PlayingCardDeckComponent> ent, ref RMCStorageEjectHandItemEvent args)
    {
        if (_net.IsServer)
            DrawCard(ent, args.User);
        args.Handled = true;
    }

    private void OnHandEjectHand(Entity<PlayingCardHandComponent> ent, ref RMCStorageEjectHandItemEvent args)
    {
        if (ent.Comp.FaceUp)
            return;
        if (_net.IsServer)
            DrawFromHand(ent, args.User);
        args.Handled = true;
    }

    #endregion

    #region Card Logic

    protected virtual void FlipCard(Entity<PlayingCardComponent> card, EntityUid user)
    {
        card.Comp.FaceUp = !card.Comp.FaceUp;
        Dirty(card);

        var direction = card.Comp.FaceUp ? "up" : "down";
        Popup.PopupPredicted(Loc.GetString("rmc-playing-card-flip", ("direction", direction)), null, card, user);
    }

    protected virtual void FlipHand(Entity<PlayingCardHandComponent> hand, EntityUid user)
    {
        hand.Comp.FaceUp = !hand.Comp.FaceUp;
        Dirty(hand);

        var direction = hand.Comp.FaceUp ? "up" : "down";
        Popup.PopupPredicted(Loc.GetString("rmc-playing-card-hand-flip", ("direction", direction)), null, hand, user);
    }

    protected virtual void CombineCards(Entity<PlayingCardComponent> card1, Entity<PlayingCardComponent> card2, EntityUid user)
    {
    }

    protected virtual void AddCardToHand(Entity<PlayingCardHandComponent> hand, Entity<PlayingCardComponent> card, EntityUid user)
    {
        hand.Comp.Cards.Add(EncodeCard(card.Comp.Suit, card.Comp.Rank));
        Dirty(hand);
        PredictedQueueDel(card.Owner);
        UpdateHandName(hand);
        TryPopup(hand, Loc.GetString("rmc-playing-card-add-to-hand", ("count", hand.Comp.Cards.Count)), user);
    }

    protected virtual void MergeHands(Entity<PlayingCardHandComponent> hand1, Entity<PlayingCardHandComponent> hand2, EntityUid user)
    {
        hand1.Comp.Cards.AddRange(hand2.Comp.Cards);
        Dirty(hand1);
        PredictedQueueDel(hand2.Owner);
        UpdateHandName(hand1);
        TryPopup(hand1, Loc.GetString("rmc-playing-card-merge-hands", ("count", hand1.Comp.Cards.Count)), user);
    }

    protected virtual void DrawFromHand(Entity<PlayingCardHandComponent> hand, EntityUid user)
    {
    }

    protected virtual void DrawSpecificCard(Entity<PlayingCardHandComponent> hand, EntityUid user, int index)
    {
    }

    protected void UpdateHandName(Entity<PlayingCardHandComponent> hand)
    {
        var name = hand.Comp.Cards.Count > StackThreshold
            ? Loc.GetString("rmc-playing-card-stack-name")
            : Loc.GetString("rmc-playing-card-hand-name");
        _meta.SetEntityName(hand, name);
    }

    protected void TryPopup(Entity<PlayingCardHandComponent> hand, string message, EntityUid user)
    {
        var curTime = _timing.CurTime;
        if (curTime < hand.Comp.LastPopupTime + TimeSpan.FromSeconds(hand.Comp.PopupCooldown))
            return;

        hand.Comp.LastPopupTime = curTime;
        Popup.PopupPredicted(message, null, hand, user);
    }

    #endregion

    #region Deck Logic

    private void InitializeDeck(Entity<PlayingCardDeckComponent> deck)
    {
        deck.Comp.CardOrder.Clear();

        // Create all 52 cards
        foreach (var suit in Enum.GetValues<CardSuit>())
        {
            foreach (var rank in Enum.GetValues<CardRank>())
            {
                deck.Comp.CardOrder.Add(EncodeCard(suit, rank));
            }
        }

        Dirty(deck);
    }

    private void ShuffleDeck(Entity<PlayingCardDeckComponent> deck)
    {
        // Fisher-Yates shuffle of remaining cards only
        for (var i = deck.Comp.CardOrder.Count - 1; i > 0; i--)
        {
            var j = _random.Next(i + 1);
            (deck.Comp.CardOrder[i], deck.Comp.CardOrder[j]) = (deck.Comp.CardOrder[j], deck.Comp.CardOrder[i]);
        }

        Dirty(deck);
    }

    private void ShuffleHand(Entity<PlayingCardHandComponent> hand)
    {
        for (var i = hand.Comp.Cards.Count - 1; i > 0; i--)
        {
            var j = _random.Next(i + 1);
            (hand.Comp.Cards[i], hand.Comp.Cards[j]) = (hand.Comp.Cards[j], hand.Comp.Cards[i]);
        }

        Dirty(hand);
    }

    protected virtual void DrawCard(Entity<PlayingCardDeckComponent> deck, EntityUid user)
    {
    }

    protected virtual void DrawMultiple(Entity<PlayingCardDeckComponent> deck, EntityUid user, int count)
    {
    }

    protected virtual void AddCardToDeck(Entity<PlayingCardDeckComponent> deck, Entity<PlayingCardComponent> card, EntityUid user)
    {
        if (deck.Comp.CardOrder.Count >= deck.Comp.MaxCards)
        {
            Popup.PopupPredicted(Loc.GetString("rmc-playing-card-deck-full"), null, deck, user);
            return;
        }

        deck.Comp.CardOrder.Add(EncodeCard(card.Comp.Suit, card.Comp.Rank));
        Dirty(deck);
        PredictedQueueDel(card.Owner);

        Popup.PopupPredicted(Loc.GetString("rmc-playing-card-added-to-deck"), null, deck, user);
        Audio.PlayPredicted(deck.Comp.DrawSound, deck, user);
    }

    protected virtual void AddHandToDeck(Entity<PlayingCardDeckComponent> deck, Entity<PlayingCardHandComponent> hand, EntityUid user)
    {
        if (deck.Comp.CardOrder.Count >= deck.Comp.MaxCards)
        {
            Popup.PopupPredicted(Loc.GetString("rmc-playing-card-deck-full"), null, deck, user);
            return;
        }

        var added = 0;
        foreach (var card in hand.Comp.Cards)
        {
            if (deck.Comp.CardOrder.Count >= deck.Comp.MaxCards)
                break;
            deck.Comp.CardOrder.Add(card);
            added++;
        }

        hand.Comp.Cards.RemoveRange(0, added);
        Dirty(deck);

        if (hand.Comp.Cards.Count == 0)
            PredictedQueueDel(hand.Owner);
        else
            Dirty(hand);

        if (added > 0)
        {
            Popup.PopupPredicted(Loc.GetString("rmc-playing-card-added-cards-to-deck", ("count", added)), null, deck, user);
            Audio.PlayPredicted(deck.Comp.DrawSound, deck, user);
        }
    }

    #endregion

    #region Helpers

    protected static int EncodeCard(CardSuit suit, CardRank rank)
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
            >= CardRank.Two and <= CardRank.Ten => ((int)rank).ToString(),
            CardRank.Jack => Loc.GetString("rmc-playing-card-rank-jack"),
            CardRank.Queen => Loc.GetString("rmc-playing-card-rank-queen"),
            CardRank.King => Loc.GetString("rmc-playing-card-rank-king"),
            _ => ((int)rank).ToString()
        };
    }

    private string GetRankShortName(CardRank rank)
    {
        return rank switch
        {
            CardRank.Ace => Loc.GetString("rmc-playing-card-rank-ace-short"),
            >= CardRank.Two and <= CardRank.Ten => ((int)rank).ToString(),
            CardRank.Jack => Loc.GetString("rmc-playing-card-rank-jack-short"),
            CardRank.Queen => Loc.GetString("rmc-playing-card-rank-queen-short"),
            CardRank.King => Loc.GetString("rmc-playing-card-rank-king-short"),
            _ => ((int)rank).ToString()
        };
    }

    public static string GetCardStateName(CardSuit suit, CardRank rank)
    {
        return $"{suit.ToString().ToLowerInvariant()}_{rank.ToString().ToLowerInvariant()}";
    }

    #endregion
}
