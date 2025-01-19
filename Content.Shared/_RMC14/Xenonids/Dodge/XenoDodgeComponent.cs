using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.Dodge;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(XenoDodgeSystem))]
public sealed partial class XenoDodgeComponent : Component
{
    [DataField, AutoNetworkedField]
    public int PlasmaCost = 200;

    [DataField, AutoNetworkedField]
    public TimeSpan Duration = TimeSpan.FromSeconds(7);
}
