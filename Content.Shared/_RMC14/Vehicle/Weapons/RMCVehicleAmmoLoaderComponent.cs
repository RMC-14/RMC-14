using System;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Vehicle;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCVehicleAmmoLoaderComponent : Component
{
    [DataField, AutoNetworkedField]
    public string HardpointType = string.Empty;

    [DataField, AutoNetworkedField]
    public EntProtoId? BulletType;

    [DataField, AutoNetworkedField]
    public TimeSpan LoadDelay = TimeSpan.FromSeconds(1.5);
}
