using Robust.Shared.GameStates;

namespace Content.Shared._CM14.Xenos.Paralyzing;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(XenoParalyzingSlashSystem))]
public sealed partial class XenoActiveParalyzingSlashComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan ExpireAt;

    [DataField, AutoNetworkedField]
    public TimeSpan StunDelay = TimeSpan.FromSeconds(4);

    [DataField, AutoNetworkedField]
    public TimeSpan StunDuration = TimeSpan.FromSeconds(4);
}
