using Content.Shared._RMC14.WeedKiller;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Areas;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(AreaSystem))]
public sealed partial class AreaComponent : Component
{
    [DataField("CAS"), AutoNetworkedField]
    public bool CAS;

    [DataField, AutoNetworkedField]
    public bool Fulton;

    [DataField, AutoNetworkedField]
    public bool Lasing;

    [DataField, AutoNetworkedField]
    public bool MortarPlacement;

    [DataField, AutoNetworkedField]
    public bool MortarFire;

    [DataField, AutoNetworkedField]
    public bool Medevac;

    [DataField, AutoNetworkedField]
    public bool Paradropping;

    [DataField("OB"), AutoNetworkedField]
    public bool OB;

    [DataField, AutoNetworkedField]
    public bool SupplyDrop;

    [DataField, AutoNetworkedField]
    public bool AvoidBioscan;

    [DataField, AutoNetworkedField]
    public bool NoTunnel;

    [DataField, AutoNetworkedField]
    public bool Unweedable;

    [DataField, AutoNetworkedField]
    public bool BuildSpecial;

    [DataField, AutoNetworkedField]
    public bool ResinAllowed = true;

    [DataField, AutoNetworkedField]
    public bool ResinConstructionAllowed = true;

    [DataField, AutoNetworkedField]
    public bool WeatherEnabled = true;

    [DataField, AutoNetworkedField]
    public bool HijackEvacuationArea;

    [DataField, AutoNetworkedField]
    public bool AlwaysPowered = false;

    // TODO RMC14 does this need to be a double?
    [DataField, AutoNetworkedField]
    public double HijackEvacuationWeight;

    [DataField, AutoNetworkedField]
    public AreaHijackEvacuationType HijackEvacuationType = AreaHijackEvacuationType.None;

    [DataField, AutoNetworkedField]
    public string? PowerNet;

    [DataField, AutoNetworkedField]
    public Color MinimapColor;

    [DataField, AutoNetworkedField]
    public int ZLevel;

    [DataField, AutoNetworkedField]
    public bool LandingZone;

    [DataField, AutoNetworkedField]
    [Access(typeof(AreaSystem), typeof(WeedKillerSystem))]
    public string? LinkedLz;

    [DataField, AutoNetworkedField]
    [Access(typeof(AreaSystem), typeof(WeedKillerSystem))]
    public bool WeedKilling;

    [DataField, AutoNetworkedField]
    public bool RetrieveItemObjective;
}
