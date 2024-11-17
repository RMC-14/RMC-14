using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.FarSight;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(FarSightSystem))]
public sealed partial class FarSightComponent : Component
{
    // The item responsible for granting the user farsight
    [ViewVariables, AutoNetworkedField]
    public EntityUid Item;
}
