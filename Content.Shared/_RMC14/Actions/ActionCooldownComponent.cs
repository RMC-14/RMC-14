using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Actions;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedRMCActionsSystem))]
public sealed partial class ActionCooldownComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan Cooldown;
}
