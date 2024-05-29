using Robust.Shared.GameStates;

namespace Content.Shared._CM14.Xenos.Weeds;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class OnXenoWeedsComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool OnXenoWeeds;
}
