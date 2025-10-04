using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Medical.Defibrillator;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(CMDefibrillatorSystem))]
public sealed partial class CMDefibrillatorBlockedComponent : Component
{
    [DataField]
    public LocId Popup = "rmc-defibrillator-unrevivable";

    [DataField]
    public LocId Examine = "rmc-defibrillator-unrevivable";

    [DataField, AutoNetworkedField]
    public bool ShowOnExamine = false;
}
