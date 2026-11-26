using Robust.Shared.Utility;

namespace Content.Shared._RMC14.TacticalMap;

[RegisterComponent]
[Access(typeof(SharedTacticalMapSystem))]
public sealed partial class ActiveTacticalMapTrackedComponent : Component
{
    [ViewVariables]
    public EntityUid? Map;

    [ViewVariables]
    public Vector2i Indices;

    [ViewVariables]
    public SpriteSpecifier.Rsi? Icon;

    [ViewVariables]
    public SpriteSpecifier.Rsi? Background;

    [ViewVariables]
    public Color Color = Color.White;

    [ViewVariables]
    public float Opacity = 1f;

    [ViewVariables]
    public TacticalMapBlipFlags Flags;

    [ViewVariables]
    public TimeSpan UnrevivableStart;

    [ViewVariables]
    public TimeSpan UnrevivableEnd;

    [ViewVariables]
    public string? Name;

    [ViewVariables]
    public string? Role;

    [ViewVariables]
    public HashSet<EntityUid> Channels = new();

    [ViewVariables]
    public bool MembershipDirty = true;

    [ViewVariables]
    public bool AppearanceDirty = true;
}
