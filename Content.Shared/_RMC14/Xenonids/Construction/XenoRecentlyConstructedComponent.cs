using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.Construction;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedXenoConstructionSystem))]
public sealed partial class XenoRecentlyConstructedComponent : Component
{
    [DataField, AutoNetworkedField]
    public List<EntityUid> StopCollide = new();

    [DataField, AutoNetworkedField]
    public TimeSpan ExpireAt;
}
