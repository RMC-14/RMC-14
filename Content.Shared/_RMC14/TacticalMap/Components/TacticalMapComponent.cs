using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.TacticalMap;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedTacticalMapSystem))]
public sealed partial class TacticalMapComponent : Component
{
    [DataField]
    public string DisplayName = string.Empty;

    [ViewVariables]
    public Dictionary<Vector2i, TacticalMapChunk> Chunks = new();

    [ViewVariables]
    public Dictionary<Vector2i, string> AreaLabels = new();

    [ViewVariables]
    public bool HasBounds;

    [ViewVariables]
    public Vector2i BoundsMin;

    [ViewVariables]
    public Vector2i BoundsMax;

    [ViewVariables]
    public bool BaseDirty = true;

    [ViewVariables]
    public bool RoofsDirty = true;

    [ViewVariables]
    public Dictionary<Vector2i, TacticalMapTileFlags> RoofBlocked = new();

    [ViewVariables]
    public Dictionary<TacticalMapChannelKey, EntityUid> Channels = new();

    [ViewVariables]
    public int Version;
}

public readonly record struct TacticalMapChannelKey(string Prototype, EntityUid? Owner);

[Serializable, NetSerializable]
public sealed class TacticalMapComponentState(
    string displayName,
    Dictionary<Vector2i, TacticalMapChunkData> chunks,
    Dictionary<Vector2i, string> areaLabels,
    bool hasBounds,
    Vector2i boundsMin,
    Vector2i boundsMax) : ComponentState
{
    public string DisplayName = displayName;
    public readonly Dictionary<Vector2i, TacticalMapChunkData> Chunks = chunks;
    public readonly Dictionary<Vector2i, string> AreaLabels = areaLabels;
    public bool HasBounds = hasBounds;
    public Vector2i BoundsMin = boundsMin;
    public Vector2i BoundsMax = boundsMax;
}

[Serializable, NetSerializable]
public sealed class TacticalMapComponentDeltaState(
    string displayName,
    Dictionary<Vector2i, TacticalMapChunkData> modifiedChunks,
    HashSet<Vector2i> allChunks,
    Dictionary<Vector2i, string> areaLabels,
    bool hasBounds,
    Vector2i boundsMin,
    Vector2i boundsMax) : ComponentState, IComponentDeltaState<TacticalMapComponentState>
{
    public readonly string DisplayName = displayName;
    public readonly Dictionary<Vector2i, TacticalMapChunkData> ModifiedChunks = modifiedChunks;
    public readonly HashSet<Vector2i> AllChunks = allChunks;
    public readonly Dictionary<Vector2i, string> AreaLabels = areaLabels;
    public readonly bool HasBounds = hasBounds;
    public readonly Vector2i BoundsMin = boundsMin;
    public readonly Vector2i BoundsMax = boundsMax;

    public void ApplyToFullState(TacticalMapComponentState state)
    {
        foreach (var key in state.Chunks.Keys)
        {
            if (!AllChunks.Contains(key))
                state.Chunks.Remove(key);
        }

        foreach (var (key, chunk) in ModifiedChunks)
        {
            state.Chunks[key] = chunk;
        }

        state.AreaLabels.Clear();
        foreach (var (key, label) in AreaLabels)
        {
            state.AreaLabels[key] = label;
        }

        state.DisplayName = DisplayName;
        state.HasBounds = HasBounds;
        state.BoundsMin = BoundsMin;
        state.BoundsMax = BoundsMax;
    }

    public TacticalMapComponentState CreateNewFullState(TacticalMapComponentState state)
    {
        var chunks = new Dictionary<Vector2i, TacticalMapChunkData>(AllChunks.Count);
        foreach (var key in AllChunks)
        {
            if (ModifiedChunks.TryGetValue(key, out var modified))
                chunks[key] = modified;
            else if (state.Chunks.TryGetValue(key, out var existing))
                chunks[key] = existing;
        }

        return new TacticalMapComponentState(
            DisplayName,
            chunks,
            new Dictionary<Vector2i, string>(AreaLabels),
            HasBounds,
            BoundsMin,
            BoundsMax);
    }
}
