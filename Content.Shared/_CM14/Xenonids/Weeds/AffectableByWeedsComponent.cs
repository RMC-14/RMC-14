using Robust.Shared.GameStates;

namespace Content.Shared._CM14.Xenonids.Weeds;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedXenoWeedsSystem))]
public sealed partial class AffectableByWeedsComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool OnXenoWeeds;
}
