using Content.Shared._RMC14.TacticalMap;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.Construction;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(XenoRemoteStructureRemovalSystem))]
public sealed partial class XenoRemoteStructureRemovalComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan RemovalDelay = TimeSpan.FromMinutes(5);
}
