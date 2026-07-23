using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Drowsyness;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(DrowsynessSystem))]
public sealed partial class DrowsynessComponent : Component
{
    [DataField, AutoNetworkedField]
    public FixedPoint2 Amount;
}
