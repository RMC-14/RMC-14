using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.PowerLoader;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(PowerLoaderSystem))]
public sealed partial class PowerLoaderVirtualItemComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? Grabbed;
}
