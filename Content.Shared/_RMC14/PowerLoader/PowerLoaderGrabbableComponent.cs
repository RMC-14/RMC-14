using Content.Shared._RMC14.Chair;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.PowerLoader;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(PowerLoaderSystem), typeof(SharedRMCChairStackSystem))]
public sealed partial class PowerLoaderGrabbableComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan Delay;

    [DataField, AutoNetworkedField]
    public EntProtoId VirtualRight;

    [DataField, AutoNetworkedField]
    public EntProtoId VirtualLeft;
}
