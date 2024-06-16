using Robust.Shared.GameStates;

namespace Content.Shared._CM14.Xenos.Announce;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedXenoAnnounceSystem))]
public sealed partial class XenoAnnounceDeathComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public LocId Message;

    [DataField, AutoNetworkedField]
    public Color Color = Color.FromHex("#2A623D");
}
