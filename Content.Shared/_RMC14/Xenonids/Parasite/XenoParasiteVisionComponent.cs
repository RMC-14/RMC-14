using Robust.Shared.GameStates;
using System.Numerics;

namespace Content.Shared._RMC14.Xenonids.Parasite;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(XenoParasiteVisionSystem))]
public sealed partial class XenoParasiteVisionComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Enabled;

    [DataField, AutoNetworkedField]
    public Vector2 Zoom = new(1.5f, 1.5f);

    [DataField, AutoNetworkedField]
    public int OffsetLength;

    [DataField, AutoNetworkedField]
    public Vector2 Offset;

    [DataField, AutoNetworkedField]
    public float Speed = 0f;


}