using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.PlayingCards;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedPlayingCardSystem))]
public sealed partial class PlayingCardComponent : Component
{
    /// The suit of the card (spades, hearts, diamonds, clubs).
    [DataField(required: true), AutoNetworkedField]
    public CardSuit Suit = CardSuit.Spades;

    /// The rank of the card (ace, 2-10, jack, queen, king).
    [DataField(required: true), AutoNetworkedField]
    public CardRank Rank = CardRank.Ace;

    /// Whether the card is face up (showing its value) or face down.
    [DataField, AutoNetworkedField]
    public bool FaceUp = true;
}

[Serializable, NetSerializable]
public enum CardSuit : byte
{
    Spades,
    Hearts,
    Diamonds,
    Clubs
}

[Serializable, NetSerializable]
public enum CardRank : byte
{
    Ace = 1,
    Two = 2,
    Three = 3,
    Four = 4,
    Five = 5,
    Six = 6,
    Seven = 7,
    Eight = 8,
    Nine = 9,
    Ten = 10,
    Jack = 11,
    Queen = 12,
    King = 13
}
