using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Medical.Refill;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RMCMedLinkRestockerSystem))]
public sealed partial class RMCMedLinkRestockerComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool AllowSupplyLinkRestock = true;

    [DataField, AutoNetworkedField]
    public int RestockMinimumRoundTime = 20; //Minutes
}
