using System.Collections.Generic;
using System.Numerics;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Effects;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCSpriteOffsetAnimationComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public Vector2 StartingOffset;

    [DataField, AutoNetworkedField]
    public Vector2? EndingOffset;

    [DataField, AutoNetworkedField]
    public List<Vector2>? PathOffsets;

    [DataField, AutoNetworkedField]
    public float? Length;
}
