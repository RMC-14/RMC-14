using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Vehicle;

[Prototype]
public sealed partial class HardpointVehicleFamilyPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = string.Empty;
}
