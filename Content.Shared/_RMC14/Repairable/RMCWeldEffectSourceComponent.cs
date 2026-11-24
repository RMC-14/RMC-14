using System.Numerics;
using System.Collections.Generic;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Repairable;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCWeldEffectSourceComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public Vector2 StartingOffset;

    [DataField, AutoNetworkedField]
    public Vector2? EndingOffset;

    [DataField, AutoNetworkedField]
    public List<Vector2>? PathOffsets;
}
