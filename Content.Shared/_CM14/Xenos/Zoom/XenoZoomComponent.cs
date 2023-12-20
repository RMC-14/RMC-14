using System.Numerics;
using Robust.Shared.GameStates;

namespace Content.Shared._CM14.Xenos.Zoom;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(XenoZoomSystem))]
public sealed partial class XenoZoomComponent : Component
{
    [DataField, AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    public bool Enabled;

    [DataField, AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    public Vector2 Zoom = new(1.25f, 1.25f);
}
