using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Botany;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedRMCPlantTraySystem))]
public sealed partial class RMCPlantSampleTakerComponent : Component { }
