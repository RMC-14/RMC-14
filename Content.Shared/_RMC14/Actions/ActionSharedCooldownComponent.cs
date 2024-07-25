using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Actions;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RMCActionsSystem))]
public sealed partial class ActionSharedCooldownComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public HashSet<EntProtoId> Ids;

    [DataField, AutoNetworkedField]
    public TimeSpan Cooldown;

    [DataField, AutoNetworkedField]
    public bool OnPerform = true;
}
