using Robust.Shared.GameStates;
using Content.Shared.Atmos;
using Robust.Shared.Audio;

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

    [DataField, AutoNetworkedField]
    public SoundSpecifier? TravelSound = new SoundCollectionSpecifier("XenoVentCrawl");
}
