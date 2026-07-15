using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Repairable;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RMCWeldableSystem))]
public sealed partial class RMCWeldFuelComponent : Component
{
    [DataField, AutoNetworkedField]
    public float WeldFuelMultiplier = 0.33f;

    [DataField, AutoNetworkedField]
    public float MinWeldFuel = 0f;
}
