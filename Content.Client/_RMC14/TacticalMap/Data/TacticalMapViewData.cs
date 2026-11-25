using Content.Shared._RMC14.Marines.Squads;
using Content.Shared._RMC14.TacticalMap;

namespace Content.Client._RMC14.TacticalMap;

public readonly record struct TacticalMapBlipView(NetEntity Entity, TacticalMapBlip Blip);

public sealed class TacticalMapCanvasLayer
{
    public EntityUid Channel;
    public string Name = string.Empty;
    public Color Color = Color.White;
    public int SortOrder;
    public TacticalMapChannelScope Scope;
    public IReadOnlyList<TacticalMapStroke> Strokes = Array.Empty<TacticalMapStroke>();
    public IReadOnlyDictionary<Vector2i, TacticalMapLabelData> Labels = new Dictionary<Vector2i, TacticalMapLabelData>();
    public int Version;
    public TimeSpan LastSubmitAt;
    public TimeSpan NextSubmitAt;
    public bool Drawable;
    public IReadOnlyDictionary<SquadObjectiveType, string>? Objectives;
}

public readonly record struct TacticalMapMapOption(EntityUid Map, string Name);
