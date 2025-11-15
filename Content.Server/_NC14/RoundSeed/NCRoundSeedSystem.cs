using Content.Server.Administration.Logs;
using Content.Server.GameTicking.Events;
using Content.Shared._NC14.RoundSeed;
using Content.Shared.Database;
using Content.Shared.GameTicking;
using Robust.Server.GameStates;
using Robust.Shared.Localization;
using Robust.Shared.Map;
using Robust.Shared.Random;
using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;

namespace Content.Server._NC14.RoundSeed;

public sealed class NCRoundSeedSystem : SharedNCRoundSeedSystem
{
    [Dependency] private readonly IAdminLogManager _adminLogs = default!;
    [Dependency] private readonly PvsOverrideSystem _pvsOverride = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    private EntityUid? _seedEntity;
    private string? _queuedSeedText;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundStartingEvent>(OnRoundStarting);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestartCleanup);
    }

    private void OnRoundStarting(RoundStartingEvent ev)
    {
        var (seed, queued, sourceText) = EnsureSeed();
        LogSeed(ev.Id, seed, queued, sourceText);
    }

    private void OnRoundRestartCleanup(RoundRestartCleanupEvent ev)
    {
        var tracker = TryGetTracker();
        if (tracker != null)
            QueueDel(tracker.Value);

        _seedEntity = null;
        _queuedSeedText = null;
    }

    public (int Seed, bool WasQueued, string? SourceText) EnsureSeed()
    {
        if (TryGetSeed(out var seed))
            return (seed, false, null);

        var wasQueued = false;
        string? sourceText = null;

        if (_queuedSeedText is { } nextSeedText)
        {
            seed = GenerateSeedValue(nextSeedText);
            sourceText = nextSeedText;
            wasQueued = true;
            _queuedSeedText = null;
        }
        else
        {
            seed = _random.Next();
        }

        var tracker = EnsureTracker();
        tracker.Comp.Seed = seed;
        Dirty(tracker);

        return (seed, wasQueued, sourceText);
    }

    public void SetNextSeed(string seedText, string? by = null)
    {
        _queuedSeedText = seedText;

        var seedValue = GenerateSeedValue(seedText);

        var who = string.IsNullOrEmpty(by)
            ? Loc.GetString("nc14-round-seed-system-actor-server")
            : by;

        var logMessage = Loc.GetString(
            "nc14-round-seed-system-log-queued-next",
            ("by", who),
            ("seedText", seedText),
            ("seedValue", seedValue));

        _adminLogs.Add(LogType.NC14RoundSeed, LogImpact.Medium, $"{logMessage}");
        Log.Info(logMessage);
    }

    private void LogSeed(int roundId, int seed, bool queued, string? sourceText)
    {
        var logMessage = queued
            ? Loc.GetString(
                "nc14-round-seed-system-log-seed-queued",
                ("roundId", roundId),
                ("seedValue", seed),
                ("seedText", sourceText ?? seed.ToString()))
            : Loc.GetString(
                "nc14-round-seed-system-log-seed-generated",
                ("roundId", roundId),
                ("seedValue", seed));

        _adminLogs.Add(LogType.NC14RoundSeed, LogImpact.Low, $"{logMessage}");
        Log.Info(logMessage);
    }

    private static int GenerateSeedValue(string seedText)
    {
        if (int.TryParse(seedText, out var numericSeed))
            return numericSeed;

        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(seedText));

        // Use first 4 bytes for a deterministic 32-bit seed
        return BinaryPrimitives.ReadInt32LittleEndian(hash.AsSpan());
    }

    private Entity<NCRoundSeedComponent> EnsureTracker()
    {
        if (_seedEntity is { } existing &&
            TryComp(existing, out NCRoundSeedComponent? existingTracker))
        {
            return (existing, existingTracker);
        }

        var tracker = TryGetTracker();
        if (tracker != null)
        {
            _seedEntity = tracker.Value;
            return tracker.Value;
        }

        var trackerEntity = SpawnSeedEntity();
        var comp = EnsureComp<NCRoundSeedComponent>(trackerEntity);
        Dirty(trackerEntity, comp);

        _seedEntity = trackerEntity;

        return (trackerEntity, comp);
    }

    private EntityUid SpawnSeedEntity()
    {
        var tracker = EntityManager.SpawnEntity(null, MapCoordinates.Nullspace);
        _pvsOverride.AddGlobalOverride(tracker);
        return tracker;
    }

    private Entity<NCRoundSeedComponent>? TryGetTracker()
    {
        var query = EntityQueryEnumerator<NCRoundSeedComponent>();
        if (!query.MoveNext(out var tracker, out var trackerComponent))
            return null;

        return (tracker, trackerComponent);
    }
}
