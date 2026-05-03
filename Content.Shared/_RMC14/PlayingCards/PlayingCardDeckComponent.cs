using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.PlayingCards;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(SharedPlayingCardSystem))]
public sealed partial class PlayingCardDeckComponent : Component
{
    [DataField, AutoNetworkedField]
    public int CardsRemaining = 52;

    [DataField, AutoNetworkedField]
    public List<int> CardOrder = new();

    [DataField]
    public EntProtoId CardPrototype = "RMCPlayingCard";

    [DataField]
    public SoundSpecifier? DrawSound = new SoundPathSpecifier("/Audio/_RMC14/Handling/paper_pickup.ogg");

    [DataField]
    public SoundSpecifier? ShuffleSound = new SoundPathSpecifier("/Audio/_RMC14/Handling/paper_drop.ogg");

    [DataField]
    public int MaxCards = 52;
}
