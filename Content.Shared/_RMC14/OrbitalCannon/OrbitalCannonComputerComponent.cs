using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.OrbitalCannon;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(OrbitalCannonSystem))]
public sealed partial class OrbitalCannonComputerComponent : Component
{
    [DataField, AutoNetworkedField]
    public string? Warhead;

    [DataField, AutoNetworkedField]
    public int Fuel;

    [DataField, AutoNetworkedField]
    public List<WarheadFuelRequirement> FuelRequirements = new();

    [DataField, AutoNetworkedField]
    public OrbitalCannonStatus Status = OrbitalCannonStatus.Unloaded;
}
