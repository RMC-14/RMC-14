using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.AntiAir;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(RMCShipAntiAirSystem))]
public sealed partial class RMCShipAntiAirComponent : Component
{
    [DataField, AutoNetworkedField]
    public string? ProtectedZone;

    [DataField, AutoNetworkedField]
    public List<RMCShipDefenseZoneEntry> Zones = new();

    [DataField, AutoNetworkedField]
    public bool Disabled;

    [DataField, AutoNetworkedField]
    public bool DisableOnHijack = true;

    [DataField, AutoNetworkedField]
    public SoundSpecifier DeterrenceSound = new SoundPathSpecifier("/Audio/_RMC14/Effects/antiair_explosions.ogg");

    [DataField, AutoNetworkedField]
    public int DeterrenceShakeIntensity = 60;

    [DataField, AutoNetworkedField]
    public int DeterrenceShakeDuration = 2;
}
