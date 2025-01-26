using Robust.Shared.GameStates;
using Robust.Shared.Map;

namespace Content.Shared._RMC14.Atmos;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedRMCFlammableSystem))]
public sealed partial class SteppingOnFireComponent : Component
{
    [DataField, AutoNetworkedField]
    public double ArmorMultiplier = 1;

    [DataField, AutoNetworkedField]
    public float Distance;

    public EntityCoordinates? LastPosition;
}
