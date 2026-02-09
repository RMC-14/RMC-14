using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.PlayingCards;

/// Component for a hand of playing cards that can be combined.
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(SharedPlayingCardSystem))]
public sealed partial class PlayingCardHandComponent : Component
{
    /// The cards in this hand. List.
    [DataField, AutoNetworkedField]
    public List<int> Cards = new();

    /// Maximum cards that can be held in a hand.
    [DataField]
    public int MaxCards = 52;

    /// Whether the hand is face up or face down by default.
    [DataField, AutoNetworkedField]
    public bool FaceUp;
}
