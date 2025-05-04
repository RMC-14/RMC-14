using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Chat;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(SharedCMChatSystem))]
public sealed partial class HeadsetMultiBroadcastComponent : Component
{
    [DataField, AutoNetworkedField]
    public int Maximum = 4;

    [DataField, AutoNetworkedField]
    public TimeSpan Cooldown = TimeSpan.FromSeconds(180);

    [DataField, AutoNetworkedField, AutoPausedField]
    public TimeSpan? Last;
}
