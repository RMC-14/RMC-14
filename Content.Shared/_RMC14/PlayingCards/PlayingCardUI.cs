using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.PlayingCards;

[Serializable, NetSerializable]
public enum PlayingCardHandUi
{
    Key,
}

[Serializable, NetSerializable]
public sealed class PlayingCardHandBuiMsg(int cardIndex) : BoundUserInterfaceMessage
{
    public readonly int CardIndex = cardIndex;
}

[Serializable, NetSerializable]
public sealed class PlayingCardHandBuiState : BoundUserInterfaceState;
