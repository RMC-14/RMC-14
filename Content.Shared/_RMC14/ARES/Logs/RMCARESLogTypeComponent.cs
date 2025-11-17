using Content.Shared.Access;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.ARES.Logs;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCARESLogTypeComponent : Component
{
    [DataField, AutoNetworkedField]
    public HashSet<ProtoId<AccessLevelPrototype>>? Permissions;

    [DataField, AutoNetworkedField]
    public Color? Color;
}
