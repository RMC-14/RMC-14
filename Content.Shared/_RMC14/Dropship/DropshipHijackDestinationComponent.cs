using Content.Shared._RMC14.AntiAir;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Dropship;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedDropshipSystem), typeof(RMCShipAntiAirSystem))]
public sealed partial class DropshipHijackDestinationComponent : Component
{
    [DataField, AutoNetworkedField]
    public string? DefenseZone;
}
