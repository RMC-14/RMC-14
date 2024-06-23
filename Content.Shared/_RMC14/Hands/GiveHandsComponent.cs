using Content.Shared.Hands.Components;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Hands;

[RegisterComponent, NetworkedComponent]
public sealed partial class GiveHandsComponent : Component
{
    [DataField]
    public List<GivenHand> Hands = new();
}

[DataRecord]
public record struct GivenHand(string Name, HandLocation Location);
