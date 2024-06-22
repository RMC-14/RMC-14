using Robust.Shared.GameStates;

namespace Content.Shared._CM14.Xenonids.Parasite;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedXenoParasiteSystem))]
public sealed partial class XenoParasiteComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan ManualAttachDelay = TimeSpan.FromSeconds(2);

    [DataField, AutoNetworkedField]
    public TimeSpan ParalyzeTime = TimeSpan.FromMinutes(1.5);

    [DataField, AutoNetworkedField]
    public float InfectRange = 1.5f;
}
