using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.Announce;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedXenoAnnounceSystem))]
public sealed partial class XenoAnnounceDeathComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public LocId Message;

    [DataField, AutoNetworkedField]
    public Color Color = Color.FromHex("#2A623D");
}
