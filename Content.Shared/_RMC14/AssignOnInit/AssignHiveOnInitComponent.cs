using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.AssignOnInit;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(AssignOnInitSystem))]
public sealed partial class AssignHiveOnInitComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public EntityWhitelist? Whitelist;
}
