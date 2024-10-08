using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Water;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RMCWaterSystem))]
public sealed partial class WaterLinkComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public string? Id;
}
