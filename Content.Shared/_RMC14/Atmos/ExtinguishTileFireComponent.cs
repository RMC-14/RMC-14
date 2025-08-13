using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Atmos;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SprayExtinguishTileFireComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Extinguished = false;

    [DataField, AutoNetworkedField]
    public TimeSpan ExtinguishAmount = TimeSpan.FromSeconds(6);
}
