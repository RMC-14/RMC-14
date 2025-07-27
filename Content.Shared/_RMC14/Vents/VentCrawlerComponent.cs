using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Vents;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class VentCrawlerComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan VentCrawlDelay = TimeSpan.FromSeconds(0.01);

    [DataField, AutoNetworkedField]
    public TimeSpan VentEnterDelay = TimeSpan.FromSeconds(4.5);

    [DataField, AutoNetworkedField]
    public TimeSpan VentExitDelay = TimeSpan.FromSeconds(2);

}
