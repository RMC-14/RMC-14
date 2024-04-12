using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._CM14.Hands;

[RegisterComponent, NetworkedComponent]
public sealed partial class WhitelistPickupComponent : Component
{
    [DataField]
    public ComponentRegistry Any = new();
}
