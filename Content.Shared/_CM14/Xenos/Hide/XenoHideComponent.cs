using Robust.Shared.GameStates;

namespace Content.Shared._CM14.Xenos.Hide;

[RegisterComponent, NetworkedComponent]
[Access(typeof(XenoHideSystem))]
public sealed partial class XenoHideComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Hiding;
}
