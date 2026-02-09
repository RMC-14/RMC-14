using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.PlayingCards;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(SharedPlayingCardSystem))]
public sealed partial class PlayingCardDeckComponent : Component
{
    /// The number of cards remaining in the deck.
    [DataField, AutoNetworkedField]
    public int CardsRemaining = 52;

    /// The shuffled order of cards in the deck.
    /// Each entry is (Suit, Rank) tuple encoded as an int.
    [DataField, AutoNetworkedField]
    public List<int> CardOrder = new();

    /// The prototype to spawn when a card is drawn.
    [DataField]
    public EntProtoId CardPrototype = "RMCPlayingCard";

    /// Sound played when drawing a card.
    [DataField]
    public SoundSpecifier? DrawSound = new SoundPathSpecifier("/Audio/_RMC14/Handling/paper_pickup.ogg");

    /// Sound played when shuffling the deck.
    [DataField]
    public SoundSpecifier? ShuffleSound = new SoundPathSpecifier("/Audio/_RMC14/Handling/paper_drop.ogg");

    /// Maximum number of cards the deck can hold.
    [DataField]
    public int MaxCards = 52;
}
