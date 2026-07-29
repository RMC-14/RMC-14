using Robust.Shared.GameStates;
using Robust.Shared.Maths;

namespace Content.Shared._RMC14.Vehicle;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCVehicleAutopilotComponent : Component
{
    [DataField, AutoNetworkedField]
    public Vector2i Direction = new Vector2i(1, 0);
}
