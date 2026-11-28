using System.Linq;
using System.Threading.Tasks;
using Content.Server.Database;
using Content.Server.GameTicking.Events;
using Content.Shared._RMC14.CCVar;
using Content.Shared._RMC14.Rules;
using Content.Shared.Chat;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server._RMC14.Rules.DistressSignal;

public sealed partial class CMDistressSignalRuleSystem
{
    private static readonly TimeSpan PersistenceRetryDelay = TimeSpan.FromSeconds(5);

    private readonly Queue<PersistenceWrite> _persistenceWrites = new();
    private Task<(int ServerId, RMCDistressSignalStateRecord State)>? _persistenceLoadTask;
    private Task? _persistenceWriteTask;
    private TimeSpan _nextPersistenceAttempt;
    private int? _persistenceServerId;
    private int _planetHistoryVersion;
    private int? _playedPlanetRound;
    private bool _persistenceStarted;
    private bool _hasLocalBalance;
    private bool _hasLocalVotingState;
    private bool _applyingPersistedBalance;

    private sealed record PersistenceWrite(Func<int, Task> Execute, Action<Task>? OnSuccess = null);

    private void BeginPersistenceLoad()
    {
        _persistenceStarted = true;
        _persistenceLoadTask = LoadPersistence(_mapVoteExcludeLast, _marinesPerXeno);
    }

    private async Task<(int ServerId, RMCDistressSignalStateRecord State)> LoadPersistence(
        int recentPlanetCount,
        float initialBalance)
    {
        var server = await _dbEntry.ServerEntity;
        var state = await _db.GetOrCreateRMCDistressSignalState(server.Id, recentPlanetCount, initialBalance);
        return (server.Id, state);
    }

    private void UpdatePersistence()
    {
        if (Timing.RealTime < _nextPersistenceAttempt)
            return;

        if (_persistenceServerId is null)
        {
            if (_persistenceLoadTask is null)
                BeginPersistenceLoad();

            if (_persistenceLoadTask is not { IsCompleted: true } load)
                return;

            _persistenceLoadTask = null;
            if (!load.IsCompletedSuccessfully)
            {
                RetryPersistence(load);
                return;
            }

            var (serverId, state) = load.GetAwaiter().GetResult();
            _persistenceServerId = serverId;
            ApplyLoadedPersistence(state);
        }

        if (_persistenceWriteTask is { } write)
        {
            if (!write.IsCompleted)
                return;

            _persistenceWriteTask = null;
            if (!write.IsCompletedSuccessfully)
            {
                RetryPersistence(write);
                return;
            }

            _persistenceWrites.Dequeue().OnSuccess?.Invoke(write);
        }

        if (_persistenceWrites.TryPeek(out var pending))
            _persistenceWriteTask = pending.Execute(_persistenceServerId.Value);
    }

    private void RetryPersistence(Task task)
    {
        _nextPersistenceAttempt = Timing.RealTime + PersistenceRetryDelay;
        Log.Error($"Failed to persist Distress Signal state: {task.Exception}");
    }

    private void ApplyLoadedPersistence(RMCDistressSignalStateRecord state)
    {
        var locallyPlayed = _lastPlanetMaps.ToArray();
        ReplaceRecentPlanets(state.RecentPlanetIds.Concat(locallyPlayed.Select(planet => planet.Id)));

        if (!_hasLocalBalance)
            ApplyPersistedBalance(state.MarinesPerXeno);
        if (_hasLocalVotingState)
            return;

        var allPlanetIds = _rmcPlanet.GetAllPlanets()
            .Select(planet => planet.Proto.ID)
            .ToHashSet();
        var carryoverVotes = state.CarryoverVotes
            .Where(vote => vote.Value > 0 && allPlanetIds.Contains(vote.Key))
            .ToDictionary();

        var candidates = _rmcPlanet.GetCandidatesInRotation();
        candidates.TryFirstOrNull(planet => planet.Proto.ID == state.SelectedPlanetId, out var selected);
        if (state.SelectedPlanetId != null && selected == null && candidates.Count > 0)
            selected = _random.Pick(candidates);

        _carryoverVotes.Clear();
        foreach (var (planetId, votes) in carryoverVotes)
        {
            _carryoverVotes[new EntProtoId<RMCPlanetMapPrototypeComponent>(planetId)] = votes;
        }

        SelectedPlanetMap = selected;
        var selectedPlanetId = selected?.Proto.ID;
        if (carryoverVotes.Count != state.CarryoverVotes.Count || selectedPlanetId != state.SelectedPlanetId)
        {
            _persistenceWrites.Enqueue(new(id =>
                _db.SetRMCDistressSignalVotingState(id, selectedPlanetId, carryoverVotes)));
        }
    }

    private void OnPersistenceRoundStarting(RoundStartingEvent ev)
    {
        if (TryGetActiveRuleEntity() == null)
            return;

        UpdatePersistence();
        SelectRandomPlanet();
    }

    private void OnMarinesPerXenoChanged(float value)
    {
        _marinesPerXeno = value;
        if (!_persistenceStarted || _applyingPersistedBalance)
            return;

        _hasLocalBalance = true;
        _persistenceWrites.Enqueue(new(id => _db.SetRMCDistressSignalBalance(id, value)));
    }

    private void ApplyPersistedBalance(float value)
    {
        _applyingPersistedBalance = true;
        try
        {
            _config.SetCVar(RMCCVars.CMMarinesPerXeno, value);
            _marinesPerXeno = value;
        }
        finally
        {
            _applyingPersistedBalance = false;
        }
    }

    private void OnMapVoteExcludeLastChanged(int value)
    {
        var previous = _mapVoteExcludeLast;
        _mapVoteExcludeLast = Math.Max(0, value);
        TrimRecentPlanets();

        if (!_persistenceStarted || _mapVoteExcludeLast <= previous)
            return;

        var count = _mapVoteExcludeLast;
        var version = _planetHistoryVersion;
        _persistenceWrites.Enqueue(new(
            id => _db.GetRecentRMCDistressSignalPlanets(id, count),
            task =>
            {
                if (_planetHistoryVersion == version)
                    ReplaceRecentPlanets(((Task<List<string>>) task).GetAwaiter().GetResult());
            }));
    }

    private void ReplaceRecentPlanets(IEnumerable<string> planetIds)
    {
        _lastPlanetMaps.Clear();
        foreach (var planetId in planetIds)
        {
            _lastPlanetMaps.Enqueue(new EntProtoId<RMCPlanetMapPrototypeComponent>(planetId));
        }

        TrimRecentPlanets();
    }

    private void TrackPlayedPlanet(EntProtoId<RMCPlanetMapPrototypeComponent> planetId)
    {
        var roundId = GameTicker.RoundId;
        var balance = _marinesPerXeno;
        _hasLocalVotingState = true;
        _planetHistoryVersion++;
        _playedPlanetRound = roundId;
        _lastPlanetMaps.Enqueue(planetId);
        TrimRecentPlanets();
        _persistenceWrites.Enqueue(new(id => _db.AddRMCDistressSignalRound(id, roundId, planetId.Id, balance)));
    }

    private void TrimRecentPlanets()
    {
        while (_lastPlanetMaps.Count > _mapVoteExcludeLast)
        {
            _lastPlanetMaps.Dequeue();
        }
    }

    private void FinishPersistentRound(int roundId, DistressSignalRuleResult result, float marinesPerXeno)
    {
        _hasLocalBalance = true;
        ApplyPersistedBalance(marinesPerXeno);
        if (_playedPlanetRound == roundId)
        {
            _playedPlanetRound = null;
            _persistenceWrites.Enqueue(new(id => _db.FinishRMCDistressSignalRound(id, roundId, (int) result, marinesPerXeno)));
        }
        else
        {
            _persistenceWrites.Enqueue(new(id => _db.SetRMCDistressSignalBalance(id, marinesPerXeno)));
        }
    }

    private void ApplyAndQueueVotingState(
        RMCPlanet? selectedPlanet,
        IReadOnlyDictionary<EntProtoId<RMCPlanetMapPrototypeComponent>, int> carryoverVotes,
        string? announcement = null)
    {
        var savedVotes = carryoverVotes
            .Where(vote => vote.Value > 0)
            .ToDictionary(vote => vote.Key.Id, vote => vote.Value);
        var selectedPlanetId = selectedPlanet?.Proto.ID;
        _hasLocalVotingState = true;
        _carryoverVotes.Clear();
        foreach (var (planetId, votes) in savedVotes)
        {
            _carryoverVotes[new EntProtoId<RMCPlanetMapPrototypeComponent>(planetId)] = votes;
        }

        SelectedPlanetMap = selectedPlanet;
        _persistenceWrites.Enqueue(new(id => _db.SetRMCDistressSignalVotingState(id, selectedPlanetId, savedVotes)));

        if (announcement != null)
            _chatManager.ChatMessageToAll(ChatChannel.Server, announcement, announcement, EntityUid.Invalid, false, true);
    }
}
