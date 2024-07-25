using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Actions;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RMCActionsSystem))]
public sealed partial class ActionSharedCooldownComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public EntProtoId Id;

    [DataField(required: true), AutoNetworkedField]
    public TimeSpan Cooldown;
}
