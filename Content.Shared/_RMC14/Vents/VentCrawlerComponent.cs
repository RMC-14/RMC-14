using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.Vents;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class VentCrawlerComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan VentCrawlDelay = TimeSpan.FromMilliseconds(1);

    [DataField, AutoNetworkedField]
    public TimeSpan VentEnterDelay = TimeSpan.FromSeconds(4.5);

    [DataField, AutoNetworkedField]
    public TimeSpan VentExitDelay = TimeSpan.FromSeconds(2);

    [DataField, AutoNetworkedField]
    public TimeSpan VentCrawlSoundDelay = TimeSpan.FromSeconds(2);

    [DataField, AutoNetworkedField]
    public string VentCrawlIcon = "unknown";

    [DataField, AutoNetworkedField]
    public LocId VentCrawlExamine = "rmc-vent-crawling-entrance-xeno";

}
