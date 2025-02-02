using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Atmos;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedRMCFlammableSystem))]
public sealed partial class IgniteOnProjectileHitComponent : Component
{
    [DataField, AutoNetworkedField]
    public int Stacks = 20;

    [DataField, AutoNetworkedField]
    public int Intensity = 30;

    [DataField, AutoNetworkedField]
    public int Duration = 20;
}
