using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.OrbitalCannon;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(OrbitalCannonSystem))]
public sealed partial class OrbitalCannonComponent : Component
{
    [DataField, AutoNetworkedField]
    public string WarheadContainer = "rmc_orbital_cannon_warhead";

    [DataField, AutoNetworkedField]
    public string FuelContainer = "rmc_orbital_cannon_fuel";

    [DataField, AutoNetworkedField]
    public EntProtoId<OrbitalCannonWarheadComponent>[] WarheadTypes =
        ["RMCOrbitalCannonWarheadExplosive", "RMCOrbitalCannonWarheadIncendiary", "RMCOrbitalCannonWarheadCluster"];

    [DataField, AutoNetworkedField]
    public int[] PossibleFuelRequirements = [4, 5, 6];

    [DataField, AutoNetworkedField]
    public List<WarheadFuelRequirement> FuelRequirements = new();

    [DataField, AutoNetworkedField]
    public CannonStatus Status = CannonStatus.Unloaded;
}

[DataRecord]
[Serializable, NetSerializable]
public readonly record struct WarheadFuelRequirement(EntProtoId<OrbitalCannonWarheadComponent> Warhead, int Fuel);

[Serializable, NetSerializable]
public enum CannonStatus
{
    Unloaded = 0,
    Loaded,
    Chambered,
}
