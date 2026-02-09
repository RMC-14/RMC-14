using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.PlayingCards;

[Serializable, NetSerializable]
public sealed partial class PlayingCardDeckPickupDoAfterEvent : DoAfterEvent
{
    [DataField("entities", required: true)]
    public IReadOnlyList<NetEntity> Entities = default!;

    private PlayingCardDeckPickupDoAfterEvent()
    {
    }

    public PlayingCardDeckPickupDoAfterEvent(List<NetEntity> entities)
    {
        Entities = entities;
    }

    public override DoAfterEvent Clone() => this;
}
