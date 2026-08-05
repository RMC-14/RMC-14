using System.Linq;
using System.Threading.Tasks;
using Content.Server.Database;
using Content.Server.GameTicking;
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
    private Task<(int ServerId, RMCDistressSignalStateRecord State)>? _persistenceLoadTask;
    private bool _persistenceLoaded;
    private bool _persistenceInitialized;
    private bool _applyingPersistedBalance;
    private int _persistenceServerId;
    private float _persistedMarinesPerXeno;
    private PendingRoundFinalization? _pendingRoundFinalization;
    private PendingVotingState? _pendingVotingState;

    private sealed record PendingRoundFinalization(int RoundId, int Result, float MarinesPerXeno);

    private sealed record PendingVotingState(
        string? SelectedPlanetId,
        RMCPlanet? SelectedPlanet,
        Dictionary<string, int> CarryoverVotes,
        string? Announcement);

    private void BeginPersistenceLoad()
    {
        if (_persistenceLoaded || _persistenceLoadTask != null)
            return;

        _persistenceLoadTask = LoadPersistence();

        async Task<(int ServerId, RMCDistressSignalStateRecord State)> LoadPersistence()
        {
            var server = await _dbEntry.ServerEntity;
            var state = await _db.GetOrCreateRMCDistressSignalState(
                server.Id,
                _mapVoteExcludeLast,
                _marinesPerXeno);
            return (server.Id, state);
        }
    }

    private bool TryPreparePersistence(bool block = false)
    {
        if (!_persistenceLoaded)
        {
            BeginPersistenceLoad();
            var loadTask = _persistenceLoadTask;
            if (loadTask == null)
                return false;

            try
            {
                if (block && !loadTask.IsCompleted)
                    _task.BlockWaitOnTask(loadTask);

                if (!loadTask.IsCompleted)
                    return false;

                var loaded = loadTask.GetAwaiter().GetResult();
                ApplyLoadedPersistence(loaded.ServerId, loaded.State);
                _persistenceLoaded = true;
                _persistenceInitialized = true;
                _persistenceLoadTask = null;
            }
            catch (Exception e)
            {
                Log.Error($"Failed to load Distress Signal persistence:\n{e}");
                _persistenceLoadTask = null;
                return false;
            }
        }

        return TryFlushPendingPersistence();
    }

    private void ApplyLoadedPersistence(int serverId, RMCDistressSignalStateRecord state)
    {
        _persistenceServerId = serverId;
        ReplaceRecentPlanets(state.RecentPlanetIds);

        var allPlanetIds = _rmcPlanet.GetAllPlanets()
            .Select(p => p.Proto.ID)
            .ToHashSet();
        var carryoverVotes = state.CarryoverVotes
            .Where(v => v.Value > 0 && allPlanetIds.Contains(v.Key))
            .ToDictionary();

        var candidates = _rmcPlanet.GetCandidatesInRotation();
        candidates.TryFirstOrNull(p => p.Proto.ID == state.SelectedPlanetId, out var selected);
        if (state.SelectedPlanetId != null && selected == null && candidates.Count > 0)
            selected = _random.Pick(candidates);

        var selectedPlanetId = selected?.Proto.ID;

        _carryoverVotes.Clear();
        foreach (var (planetId, votes) in carryoverVotes)
        {
            _carryoverVotes[new EntProtoId<RMCPlanetMapPrototypeComponent>(planetId)] = votes;
        }

        SelectedPlanetMap = selected;
        ApplyPersistedBalance(state.MarinesPerXeno);

        if (carryoverVotes.Count != state.CarryoverVotes.Count || selectedPlanetId != state.SelectedPlanetId)
        {
            WaitForPersistence(() => _db.SetRMCDistressSignalVotingState(
                serverId,
                selectedPlanetId,
                carryoverVotes));
        }
    }

    private bool TryFlushPendingPersistence()
    {
        if (!_persistenceLoaded)
            return false;

        try
        {
            if (_pendingRoundFinalization is { } round)
            {
                var balance = WaitForPersistence(() => _db.FinishRMCDistressSignalRound(
                    _persistenceServerId,
                    round.RoundId,
                    round.Result,
                    round.MarinesPerXeno));
                ApplyPersistedBalance(balance);
                _pendingRoundFinalization = null;
            }

            if (_pendingVotingState is { } voting)
            {
                WaitForPersistence(() => _db.SetRMCDistressSignalVotingState(
                    _persistenceServerId,
                    voting.SelectedPlanetId,
                    voting.CarryoverVotes));
                ApplyVotingState(voting);
                _pendingVotingState = null;
            }

            return true;
        }
        catch (Exception e)
        {
            Log.Error($"Failed to flush pending Distress Signal persistence:\n{e}");
            InvalidatePersistence();
            return false;
        }
    }

    private void InvalidatePersistence()
    {
        _persistenceLoaded = false;
        _persistenceLoadTask = null;
    }

    private void OnPersistenceRoundStarting(RoundStartingEvent ev)
    {
        if (TryGetActiveRuleEntity() == null)
            return;

        if (TryPreparePersistence())
            SelectRandomPlanet();
    }

    protected override void BeforeStartAttempt(RoundStartAttemptEvent ev)
    {
        if (TryGetActiveRuleEntity() == null)
            return;

        if (TryPreparePersistence())
            return;

        var message = Loc.GetString("rmc-distress-signal-persistence-unavailable");
        _chatManager.SendAdminAnnouncement(message);
        _chatManager.DispatchServerAnnouncement(message);
        ev.Cancel();
    }

    private void OnMarinesPerXenoChanged(float value)
    {
        _marinesPerXeno = value;

        if (_applyingPersistedBalance || !_persistenceInitialized)
            return;

        try
        {
            if (!TryPreparePersistence(block: true))
                throw new InvalidOperationException("Distress Signal persistence is unavailable.");

            WaitForPersistence(() => _db.SetRMCDistressSignalBalance(_persistenceServerId, value));
            ApplyPersistedBalance(value);
        }
        catch (Exception e)
        {
            Log.Error($"Failed to persist manual Distress Signal balance change:\n{e}");
            ApplyPersistedBalance(_persistedMarinesPerXeno);
            InvalidatePersistence();
        }
    }

    private void ApplyPersistedBalance(float value)
    {
        _persistedMarinesPerXeno = value;
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

        if (!_persistenceLoaded || _mapVoteExcludeLast <= previous)
            return;

        try
        {
            var planets = WaitForPersistence(() => _db.GetRecentRMCDistressSignalPlanets(
                _persistenceServerId,
                _mapVoteExcludeLast));
            ReplaceRecentPlanets(planets);
        }
        catch (Exception e)
        {
            Log.Error($"Failed to reload Distress Signal planet history:\n{e}");
            InvalidatePersistence();
        }
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
        if (!TryPreparePersistence(block: true))
            throw new InvalidOperationException("Distress Signal persistence is unavailable.");

        try
        {
            WaitForPersistence(() => _db.AddRMCDistressSignalRound(
                _persistenceServerId,
                GameTicker.RoundId,
                planetId.Id,
                _marinesPerXeno));
        }
        catch
        {
            InvalidatePersistence();
            throw;
        }

        _lastPlanetMaps.Enqueue(planetId);
        TrimRecentPlanets();
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
        var pending = new PendingRoundFinalization(roundId, (int) result, marinesPerXeno);
        try
        {
            if (!TryPreparePersistence(block: true))
                throw new InvalidOperationException("Distress Signal persistence is unavailable.");

            var balance = WaitForPersistence(() => _db.FinishRMCDistressSignalRound(
                _persistenceServerId,
                pending.RoundId,
                pending.Result,
                pending.MarinesPerXeno));
            ApplyPersistedBalance(balance);
            _pendingRoundFinalization = null;
        }
        catch (Exception e)
        {
            Log.Error($"Failed to persist the end of Distress Signal round {roundId}:\n{e}");
            _pendingRoundFinalization = pending;
            InvalidatePersistence();
        }
    }

    private bool TryPersistVotingState(
        RMCPlanet? selectedPlanet,
        IReadOnlyDictionary<EntProtoId<RMCPlanetMapPrototypeComponent>, int> carryoverVotes,
        bool keepPendingOnFailure,
        string? announcement = null)
    {
        var pending = new PendingVotingState(
            selectedPlanet?.Proto.ID,
            selectedPlanet,
            carryoverVotes
                .Where(v => v.Value > 0)
                .ToDictionary(v => v.Key.Id, v => v.Value),
            announcement);

        if (!TryPreparePersistence(block: true))
        {
            if (keepPendingOnFailure)
                _pendingVotingState = pending;

            return false;
        }

        try
        {
            WaitForPersistence(() => _db.SetRMCDistressSignalVotingState(
                _persistenceServerId,
                pending.SelectedPlanetId,
                pending.CarryoverVotes));
            ApplyVotingState(pending);
            _pendingVotingState = null;
            return true;
        }
        catch (Exception e)
        {
            Log.Error($"Failed to persist Distress Signal voting state:\n{e}");
            if (keepPendingOnFailure)
                _pendingVotingState = pending;

            InvalidatePersistence();
            return false;
        }
    }

    private void ApplyVotingState(PendingVotingState voting)
    {
        _carryoverVotes.Clear();
        foreach (var (planetId, votes) in voting.CarryoverVotes)
        {
            _carryoverVotes[new EntProtoId<RMCPlanetMapPrototypeComponent>(planetId)] = votes;
        }

        SelectedPlanetMap = voting.SelectedPlanet;

        if (voting.Announcement is { } announcement)
        {
            _chatManager.ChatMessageToAll(
                ChatChannel.Server,
                announcement,
                announcement,
                EntityUid.Invalid,
                hideChat: false,
                recordReplay: true);
        }
    }

    private void WaitForPersistence(Func<Task> operation)
    {
        var task = operation();
        _task.BlockWaitOnTask(task);
        task.GetAwaiter().GetResult();
    }

    private T WaitForPersistence<T>(Func<Task<T>> operation)
    {
        var task = operation();
        _task.BlockWaitOnTask(task);
        return task.GetAwaiter().GetResult();
    }
}
