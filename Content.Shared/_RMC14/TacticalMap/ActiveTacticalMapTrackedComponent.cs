using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.TacticalMap;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedTacticalMapSystem))]
public sealed partial class ActiveTacticalMapTrackedComponent : Component
{
    [DataField]
    public EntityUid? Map;

    [DataField]
    public SpriteSpecifier.Rsi? Icon;

    [DataField]
    public Color Color;

    [DataField]
    public bool Undefibbable;

    [DataField]
    public SpriteSpecifier.Rsi? Background;

    [DataField]
    public bool HiveLeader;
}
