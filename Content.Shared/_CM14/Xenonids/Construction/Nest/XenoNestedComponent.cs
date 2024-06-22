using Robust.Shared.GameStates;

namespace Content.Shared._CM14.Xenonids.Construction.Nest;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(XenoNestSystem))]
public sealed partial class XenoNestedComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid Nest;

    [DataField, AutoNetworkedField]
    public bool Detached;
}
