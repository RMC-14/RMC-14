using System.Numerics;
using Robust.Shared.GameStates;

namespace Content.Shared._CM14.Xenonids.Zoom;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(XenoZoomSystem))]
public sealed partial class XenoZoomComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Enabled;

    [DataField, AutoNetworkedField]
    public Vector2 Zoom = new(1.25f, 1.25f);
}
