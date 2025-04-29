using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.Weeds;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedXenoWeedsSystem))]
public sealed partial class AffectableByWeedsComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool OnXenoWeeds;

    [DataField, AutoNetworkedField]
    public bool OnXenoSlowResin;

    [DataField, AutoNetworkedField]
    public bool OnXenoFastResin;
}
