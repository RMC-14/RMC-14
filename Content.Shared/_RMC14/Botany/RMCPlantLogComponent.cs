using Content.Shared.Database;

namespace Content.Shared._RMC14.Botany;

[RegisterComponent]
[Access(typeof(SharedRMCPlantTraySystem))]
public sealed partial class RMCPlantLogComponent : Component
{
    [DataField]
    public LogImpact? PlantLogImpact;

    [DataField]
    public LogImpact? HarvestLogImpact;
}
