using System;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Vehicle;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCVehicleRunoverComponent : Component
{
    [AutoNetworkedField]
    public EntityUid Vehicle;

    public TimeSpan Duration;
    public TimeSpan ExpiresAt;
}
