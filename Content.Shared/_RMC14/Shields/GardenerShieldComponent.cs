using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Shields;

// The shield itself is handled by the base system and component
// This component exists solely to display the pop-up when the gardener's shield ends
[RegisterComponent, NetworkedComponent]
public sealed partial class GardenerShieldComponent : Component { }
