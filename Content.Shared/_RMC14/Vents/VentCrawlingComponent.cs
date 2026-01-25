using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Vents;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class VentCrawlingComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan NextVentMoveTime;

    [DataField, AutoNetworkedField]
    public Direction? TravelDirection;

    [DataField, AutoNetworkedField]
    public TimeSpan NextVentCrawlSound;
}
