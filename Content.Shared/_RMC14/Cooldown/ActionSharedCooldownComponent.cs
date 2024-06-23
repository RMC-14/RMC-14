using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Cooldown;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(CooldownSystem))]
public sealed partial class ActionSharedCooldownComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public string Id = string.Empty;
}
