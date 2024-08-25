using System.Numerics;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Teleporter;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedRMCTeleporterSystem))]
public sealed partial class RMCTeleporterComponent : Component
{
    [DataField, AutoNetworkedField]
    public Vector2 Adjust;
}
