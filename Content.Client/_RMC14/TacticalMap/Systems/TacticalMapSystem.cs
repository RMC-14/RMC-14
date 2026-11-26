using System.Diagnostics.CodeAnalysis;
using Content.Shared._RMC14.TacticalMap;
using Robust.Shared.GameStates;
using Robust.Shared.Map.Components;

namespace Content.Client._RMC14.TacticalMap;

public sealed class TacticalMapSystem : SharedTacticalMapSystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private readonly Dictionary<EntityUid, EntityUid> _liveFeeds = new();
    private readonly Dictionary<EntityUid, int> _userHashes = new();
    private readonly HashSet<NetEntity> _seenBlips = new();

    public int Revision { get; private set; }

    public int BlipRevision { get; private set; }

    public event Action<EntityUid, bool>? BaseMapChanged;

    public void InvalidateViews()
    {
        Revision++;
    }

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TacticalMapComponent, ComponentHandleState>(OnMapHandleState);
        SubscribeLocalEvent<TacticalMapComponent, ComponentShutdown>(OnMapShutdown);
        SubscribeLocalEvent<TacticalMapLiveFeedComponent, ComponentHandleState>(OnLiveFeedHandleState);
        SubscribeLocalEvent<TacticalMapLiveFeedComponent, ComponentShutdown>(OnLiveFeedShutdown);

        SubscribeLocalEvent<TacticalMapChannelComponent, AfterAutoHandleStateEvent>(OnChanged);
        SubscribeLocalEvent<TacticalMapChannelComponent, ComponentShutdown>(OnChanged);
        SubscribeLocalEvent<TacticalMapCanvasComponent, AfterAutoHandleStateEvent>(OnChanged);
        SubscribeLocalEvent<TacticalMapSnapshotComponent, AfterAutoHandleStateEvent>(OnBlipsChanged);
        SubscribeLocalEvent<TacticalMapObjectivesComponent, AfterAutoHandleStateEvent>(OnChanged);
        SubscribeLocalEvent<TacticalMapUserComponent, AfterAutoHandleStateEvent>(OnUserState);
        SubscribeLocalEvent<TacticalMapComputerComponent, AfterAutoHandleStateEvent>(OnChanged);
    }

    private void OnUserState(Entity<TacticalMapUserComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        var hash = ent.Comp.Live ? 1 : 0;
        foreach (var channel in ent.Comp.Channels)
        {
            hash = HashCode.Combine(hash, channel);
        }

        if (!_userHashes.TryGetValue(ent, out var previous) || previous != hash)
        {
            _userHashes[ent] = hash;
            Revision++;
        }

        BlipRevision++;
    }

    private void OnChanged<TComp, TEvent>(Entity<TComp> ent, ref TEvent args) where TComp : IComponent
    {
        Revision++;
    }

    private void OnBlipsChanged(Entity<TacticalMapSnapshotComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        BlipRevision++;
    }

    private void OnMapHandleState(Entity<TacticalMapComponent> ent, ref ComponentHandleState args)
    {
        var comp = ent.Comp;
        switch (args.Current)
        {
            case TacticalMapComponentDeltaState delta:
            {
                var remove = new List<Vector2i>();
                foreach (var key in comp.Chunks.Keys)
                {
                    if (!delta.AllChunks.Contains(key))
                        remove.Add(key);
                }

                foreach (var key in remove)
                {
                    comp.Chunks.Remove(key);
                }

                ApplyChunks(comp, delta.ModifiedChunks);
                ApplyMapInfo(comp, delta.DisplayName, delta.AreaLabels, delta.HasBounds, delta.BoundsMin, delta.BoundsMax);
                break;
            }
            case TacticalMapComponentState state:
            {
                comp.Chunks.Clear();
                ApplyChunks(comp, state.Chunks);
                ApplyMapInfo(comp, state.DisplayName, state.AreaLabels, state.HasBounds, state.BoundsMin, state.BoundsMax);
                break;
            }
            default:
                return;
        }

        comp.Version++;
        Revision++;
        BaseMapChanged?.Invoke(ent, false);
    }

    private void OnMapShutdown(Entity<TacticalMapComponent> ent, ref ComponentShutdown args)
    {
        Revision++;
        BaseMapChanged?.Invoke(ent, true);
    }

    private static void ApplyChunks(TacticalMapComponent comp, Dictionary<Vector2i, TacticalMapChunkData> chunks)
    {
        foreach (var (key, data) in chunks)
        {
            if (!comp.Chunks.TryGetValue(key, out var chunk))
            {
                chunk = new TacticalMapChunk();
                comp.Chunks[key] = chunk;
            }

            Array.Copy(data.Colors, chunk.Colors, Math.Min(data.Colors.Length, TacticalMapChunk.TileCount));
            Array.Copy(data.Flags, chunk.Flags, Math.Min(data.Flags.Length, TacticalMapChunk.TileCount));
        }
    }

    private static void ApplyMapInfo(
        TacticalMapComponent comp,
        string displayName,
        Dictionary<Vector2i, string> areaLabels,
        bool hasBounds,
        Vector2i boundsMin,
        Vector2i boundsMax)
    {
        comp.DisplayName = displayName;
        comp.AreaLabels = new Dictionary<Vector2i, string>(areaLabels);
        comp.HasBounds = hasBounds;
        comp.BoundsMin = boundsMin;
        comp.BoundsMax = boundsMax;
    }

    private void OnLiveFeedHandleState(Entity<TacticalMapLiveFeedComponent> ent, ref ComponentHandleState args)
    {
        var comp = ent.Comp;
        NetEntity channel;
        switch (args.Current)
        {
            case TacticalMapLiveFeedDeltaState delta:
            {
                var remove = new List<NetEntity>();
                foreach (var key in comp.Blips.Keys)
                {
                    if (!delta.All.Contains(key))
                        remove.Add(key);
                }

                foreach (var key in remove)
                {
                    comp.Blips.Remove(key);
                }

                foreach (var (key, blip) in delta.Modified)
                {
                    comp.Blips[key] = blip;
                }

                channel = delta.Channel;
                break;
            }
            case TacticalMapLiveFeedState state:
            {
                comp.Blips = new Dictionary<NetEntity, TacticalMapBlip>(state.Blips);
                channel = state.Channel;
                break;
            }
            default:
                return;
        }

        var channelUid = EnsureEntity<TacticalMapLiveFeedComponent>(channel, ent);
        comp.Channel = channelUid;
        if (!_liveFeeds.TryGetValue(channelUid, out var existing) || existing != ent.Owner)
        {
            _liveFeeds[channelUid] = ent;
            Revision++;
        }

        BlipRevision++;
    }

    private void OnLiveFeedShutdown(Entity<TacticalMapLiveFeedComponent> ent, ref ComponentShutdown args)
    {
        if (ent.Comp.Channel is { } channel &&
            _liveFeeds.TryGetValue(channel, out var feed) &&
            feed == ent.Owner)
        {
            _liveFeeds.Remove(channel);
        }

        Revision++;
    }

    public bool TryGetLiveFeed(EntityUid channel, [NotNullWhen(true)] out TacticalMapLiveFeedComponent? feed)
    {
        feed = null;
        return _liveFeeds.TryGetValue(channel, out var feedUid) && TryComp(feedUid, out feed);
    }

    public bool TryGetTileIndices(EntityUid ent, EntityUid map, out Vector2i indices)
    {
        indices = default;
        return TryComp(ent, out TransformComponent? xform) &&
               xform.GridUid == map &&
               TryComp(map, out MapGridComponent? grid) &&
               _transform.TryGetGridTilePosition((ent, xform), out indices, grid);
    }

    public bool TryGetViewerChannels(EntityUid viewer, List<EntityUid> channels, out bool live)
    {
        channels.Clear();
        if (!TryGetViewer(viewer, out var list, out live))
            return false;

        foreach (var channel in list)
        {
            if (HasComp<TacticalMapChannelComponent>(channel))
                channels.Add(channel);
        }

        return true;
    }

    public void CollectMaps(IReadOnlyList<EntityUid> channels, List<TacticalMapMapOption> maps)
    {
        maps.Clear();
        foreach (var channel in channels)
        {
            if (!TryComp(channel, out TacticalMapChannelComponent? comp) ||
                comp.Map is not { } map ||
                !TryComp(map, out TacticalMapComponent? mapComp))
            {
                continue;
            }

            var exists = false;
            foreach (var option in maps)
            {
                if (option.Map != map)
                    continue;

                exists = true;
                break;
            }

            if (exists)
                continue;

            var name = string.IsNullOrWhiteSpace(mapComp.DisplayName) ? Name(map) : mapComp.DisplayName;
            maps.Add(new TacticalMapMapOption(map, name));
        }

        maps.Sort(static (a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
    }

    public TimeSpan? CollectBlips(IReadOnlyList<EntityUid> channels, bool live, EntityUid map, List<TacticalMapBlipView> blips)
    {
        blips.Clear();
        _seenBlips.Clear();
        TimeSpan? snapshotTime = null;
        foreach (var channel in channels)
        {
            if (!TryComp(channel, out TacticalMapChannelComponent? comp) ||
                comp.Map != map ||
                comp.Scope == TacticalMapChannelScope.Squad)
            {
                continue;
            }

            if (live && TryGetLiveFeed(channel, out var feed))
            {
                foreach (var (net, blip) in feed.Blips)
                {
                    if (_seenBlips.Add(net))
                        blips.Add(new TacticalMapBlipView(net, blip));
                }

                continue;
            }

            if (!TryComp(channel, out TacticalMapSnapshotComponent? snapshot))
                continue;

            foreach (var (net, blip) in snapshot.Blips)
            {
                if (_seenBlips.Add(net))
                    blips.Add(new TacticalMapBlipView(net, blip));
            }

            if (snapshotTime == null || snapshot.TakenAt > snapshotTime)
                snapshotTime = snapshot.TakenAt;
        }

        return snapshotTime;
    }

    public void CollectCanvasLayers(
        EntityUid? actor,
        EntityUid viewer,
        IReadOnlyList<EntityUid> channels,
        EntityUid map,
        List<TacticalMapCanvasLayer> layers)
    {
        layers.Clear();
        foreach (var channel in channels)
        {
            if (!TryComp(channel, out TacticalMapChannelComponent? comp) ||
                comp.Map != map ||
                !TryComp(channel, out TacticalMapCanvasComponent? canvas))
            {
                continue;
            }

            layers.Add(new TacticalMapCanvasLayer
            {
                Channel = channel,
                Name = Name(channel),
                Color = comp.Color,
                SortOrder = comp.SortOrder,
                Scope = comp.Scope,
                Strokes = canvas.Strokes,
                Labels = canvas.Labels,
                Version = canvas.Version,
                LastSubmitAt = canvas.LastSubmitAt,
                NextSubmitAt = canvas.NextSubmitAt,
                Drawable = actor != null && CanDraw(actor.Value, viewer, channel),
                Objectives = CompOrNull<TacticalMapObjectivesComponent>(channel)?.Objectives,
            });
        }

        layers.Sort(static (a, b) =>
        {
            var order = a.SortOrder.CompareTo(b.SortOrder);
            return order != 0 ? order : string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
        });
    }
}
