using System.Numerics;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Buckle;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RMCBuckleSystem))]
public sealed partial class RMCBuckleOffsetComponent : Component
{
    [DataField, AutoNetworkedField]
    public Vector2 Offset;
}
