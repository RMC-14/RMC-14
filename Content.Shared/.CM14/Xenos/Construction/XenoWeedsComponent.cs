using Robust.Shared.GameStates;

namespace Content.Shared.CM14.Xenos.Construction;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedXenoConstructionSystem))]
public sealed partial class XenoWeedsComponent : Component
{
    [DataField]
    public int Range = 5;

    [DataField]
    public bool IsSource = true;

    [DataField]
    public EntityUid? Source;
}
