using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.OrbitalCannon;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(OrbitalCannonSystem))]
public sealed partial class OrbitalCannonTrayComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? LinkedCannon;

    [DataField, AutoNetworkedField]
    public string WarheadContainer = "rmc_orbital_tray_warhead";

    [DataField, AutoNetworkedField]
    public string FuelContainer = "rmc_orbital_tray_fuel";

    [DataField, AutoNetworkedField]
    public int MaxFuel = 6;

    [DataField, AutoNetworkedField]
    public EntProtoId<OrbitalCannonWarheadComponent>? WarheadType;

    [DataField, AutoNetworkedField]
    public int FuelAmount;
}

[Serializable, NetSerializable]
public enum OrbitalCannonTrayVisuals
{
    Warhead,
    Fuel,
}
