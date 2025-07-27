using Robust.Shared.GameStates;
using Content.Shared.Atmos;

namespace Content.Shared._RMC14.Vents;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class VentCrawlableComponent : Component
{
    [DataField, AutoNetworkedField]
    public string ContainerId = "rmc_vent_container";

    [DataField, AutoNetworkedField]
    public string LayerId = "default_vent";

    [DataField, AutoNetworkedField]
    public int? MaxEntities;

    [DataField, AutoNetworkedField]
    public PipeDirection TravelDirection;
}
