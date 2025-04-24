using System.Collections.Frozen;
using System.Linq;
using Content.Shared.NPC.Components;
using Content.Shared.NPC.Prototypes;
using Content.Shared.Radio;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

// ReSharper disable CheckNamespace
namespace Content.Shared.NPC.Systems;

public sealed partial class NpcFactionSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;

    public FrozenDictionary<string, FactionData> GetFactions()
    {
        return _factions;
    }

    /// <summary>
    /// Makes the source faction neutral to the target faction, 1-way.
    /// </summary>
    public void RealMakeNeutral(string source, string target)
    {
        if (!_factions.TryGetValue(source, out var sourceFaction))
        {
            Log.Error($"Unable to find faction {source}");
            return;
        }

        if (!_factions.ContainsKey(target))
        {
            Log.Error($"Unable to find faction {target}");
            return;
        }

        sourceFaction.Friendly.Remove(target);
        sourceFaction.Hostile.Remove(target);
        RealRefreshFactions();
    }

    /// <summary>
    /// Makes the source faction friendly to the target faction, 1-way.
    /// </summary>
    public void RealMakeFriendly(string source, string target)
    {
        if (!_factions.TryGetValue(source, out var sourceFaction))
        {
            Log.Error($"Unable to find faction {source}");
            return;
        }

        if (!_factions.ContainsKey(target))
        {
            Log.Error($"Unable to find faction {target}");
            return;
        }

        sourceFaction.Friendly.Add(target);
        sourceFaction.Hostile.Remove(target);
        RealRefreshFactions();
    }

    /// <summary>
    /// Makes the source faction hostile to the target faction, 1-way.
    /// </summary>
    public void RealMakeHostile(string source, string target)
    {
        if (!_factions.TryGetValue(source, out var sourceFaction))
        {
            Log.Error($"Unable to find faction {source}");
            return;
        }

        if (!_factions.ContainsKey(target))
        {
            Log.Error($"Unable to find faction {target}");
            return;
        }

        sourceFaction.Friendly.Remove(target);
        sourceFaction.Hostile.Add(target);
        RealRefreshFactions();
    }

    private void RealRefreshFactions()
    {
        // `RefreshFactions` un-does all modifications...
        // I love untested code in upstream...
        var query = AllEntityQuery<NpcFactionMemberComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            RefreshFactions((uid, comp));
            Dirty(uid, comp);
        }

        if (_net.IsServer)
            RaiseNetworkEvent(new RefreshFactionDataEvent(_factions.ToDictionary()));
    }

    public HashSet<ProtoId<NpcFactionPrototype>> GetFriendlyFactions(HashSet<ProtoId<NpcFactionPrototype>> sources)
    {
        HashSet<ProtoId<NpcFactionPrototype>> friendly = new(sources);

        foreach (var source in sources.ToList())
        {
            if (!_factions.TryGetValue(source, out var sourceFaction))
            {
                Log.Error($"Unable to find faction {source}");
                continue;
            }
            friendly.UnionWith(sourceFaction.Friendly);
        }

        return friendly;
    }

    public HashSet<ProtoId<RadioChannelPrototype>> GetFrequencies(HashSet<ProtoId<NpcFactionPrototype>> sources)
    {
        HashSet<ProtoId<RadioChannelPrototype>> frequencies = [];

        foreach (var source in sources.ToList())
        {
            if (!_proto.TryIndex(source, out var sourceFaction))
            {
                Log.Error($"Unable to find faction {source}");
                continue;
            }
            frequencies.UnionWith(sourceFaction.Channels);
        }

        return frequencies;
    }

    public void OnRefreshFactionData(RefreshFactionDataEvent args)
    {
        if (_net.IsClient)
            _factions = args.Factions.ToFrozenDictionary();
    }
}
