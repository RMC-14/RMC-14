using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Medical.Defibrillator;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(CMDefibrillatorSystem))]
public sealed partial class CMDefibrillatorBlockedComponent : Component
{
    [DataField]
    public LocId Popup = "defibrillator-unrevivable";

    [DataField]
    public LocId Examine = "rmc-defib-suicide";

    [DataField, AutoNetworkedField]
    public bool ShowOnExamine = true;
}
