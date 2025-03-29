using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.Egg;

[RegisterComponent, NetworkedComponent]
public sealed partial class EggPlantingDistanceComponent : Component
{
    [DataField]
    public float Distance = 1.5f;
}
