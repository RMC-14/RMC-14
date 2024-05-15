using Robust.Shared.GameStates;

namespace Content.Shared._CM14.Xenos.Announce;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedXenoAnnounceSystem))]
public sealed partial class XenoAnnounceDeathComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public string Message;
}
