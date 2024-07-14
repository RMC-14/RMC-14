using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Medical.Defibrillator;

[RegisterComponent, NetworkedComponent]
[Access(typeof(CMDefibrillatorSystem))]
public sealed partial class CMDefibrillatorBlockedComponent : Component
{
    [DataField]
    public LocId Popup = "defibrillator-unrevivable";
}
