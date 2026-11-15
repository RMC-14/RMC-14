using System;
using System.Collections.Generic;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Vehicle;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class VehicleAmmoLoaderComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntProtoId? HardpointType;

    [DataField, AutoNetworkedField]
    public EntProtoId? BulletType;

    [DataField, AutoNetworkedField]
    public TimeSpan LoadDelay = TimeSpan.FromSeconds(1.5);

    [DataField, AutoNetworkedField]
    public float InteractionRange = 2.5f;

    [AutoNetworkedField]
    public VehicleAmmoLoaderUiState Ui = new(new List<VehicleAmmoLoaderUiEntry>(), 0, 0, null);
}
