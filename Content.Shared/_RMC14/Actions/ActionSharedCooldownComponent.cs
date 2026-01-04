using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Actions;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedRMCActionsSystem))]
public sealed partial class ActionSharedCooldownComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntProtoId? Id;

    [DataField, AutoNetworkedField]
    public HashSet<EntProtoId> Ids = new();

    // This action can't be used at the same time as the actions in this list.
    [DataField, AutoNetworkedField]
    public HashSet<EntProtoId> ActiveIds = new();

    [DataField, AutoNetworkedField]
    public TimeSpan Cooldown;

    [DataField, AutoNetworkedField]
    public bool OnPerform = true;
}
