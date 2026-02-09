using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.PlayingCards;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(SharedPlayingCardSystem))]
public sealed partial class PlayingCardDeckComponent : Component
{
    /// <summary>
    /// The number of cards remaining in the deck.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int CardsRemaining = 52;

    /// <summary>
    /// The shuffled order of cards in the deck.
    /// Each entry is (Suit, Rank) tuple encoded as an int.
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<int> CardOrder = new();

    /// <summary>
    /// The prototype to spawn when a card is drawn.
    /// </summary>
    [DataField]
    public EntProtoId CardPrototype = "RMCPlayingCard";

    /// <summary>
    /// Sound played when drawing a card.
    /// </summary>
    [DataField]
    public SoundSpecifier? DrawSound = new SoundPathSpecifier("/Audio/_RMC14/Handling/paper_pickup.ogg");

    /// <summary>
    /// Sound played when shuffling the deck.
    /// </summary>
    [DataField]
    public SoundSpecifier? ShuffleSound = new SoundPathSpecifier("/Audio/_RMC14/Handling/paper_drop.ogg");
}
