using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Linq;
using Content.Shared.Players.PlayTimeTracking;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.PlayTimeTracking;

public abstract class SharedRMCPlayTimeManager : IPostInjectInit
{
    [Dependency] private readonly ISharedPlaytimeManager _playtime = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    private bool _jobsDataLoaded;
    private FrozenSet<ProtoId<PlayTimeTrackerPrototype>> _xenoJobs = [];
    private FrozenSet<ProtoId<PlayTimeTrackerPrototype>> _humanJobs = [];

    void IPostInjectInit.PostInject() => PostInject();

    protected virtual void PostInject()
    {
        _prototype.PrototypesReloaded += OnPrototypesReloaded;
    }

    /// <summary>
    /// Returns the total player playtime for all human jobs.
    /// </summary>
    /// <param name="playerSession">test</param>
    /// <param name="jobs"></param>
    /// <exception cref="InvalidOperationException">Thrown if the player's playtime data is not yet loaded.</exception>
    /// <returns>The total time the player has spent as a human.</returns>
    public TimeSpan GetTotalHumanPlaytime(ICommonSession playerSession)
    {
        return GetTotalPlaytime(playerSession, _humanJobs);
    }

    /// <summary>
    /// Returns the total player playtime for all xeno jobs.
    /// </summary>
    /// <param name="playerSession">test</param>
    /// <param name="jobs"></param>
    /// <exception cref="InvalidOperationException">Thrown if the player's playtime data is not yet loaded.</exception>
    /// <returns>The total time the player has spent as a xeno.</returns>
    public TimeSpan GetTotalXenoPlaytime(ICommonSession playerSession)
    {
        return GetTotalPlaytime(playerSession, _xenoJobs);
    }

    public bool IsHumanJob(string jobId)
    {
        EnsurePrototypesLoaded();
        return _humanJobs.Contains(jobId);
    }

    public bool IsXenoJob(string jobId)
    {
        EnsurePrototypesLoaded();
        return _xenoJobs.Contains(jobId);
    }

    /// <summary>
    /// Returns the total player playtime for a set of jobs.
    /// </summary>
    /// <param name="playerSession">test</param>
    /// <param name="jobs"></param>
    /// <exception cref="InvalidOperationException">Thrown if the player's playtime data is not yet loaded.</exception>
    /// <returns>The total time the player has spent in those jobs.</returns>
    public TimeSpan GetTotalPlaytime(ICommonSession playerSession, IEnumerable<ProtoId<PlayTimeTrackerPrototype>> jobs)
    {
        EnsurePrototypesLoaded();

        var playTimes = _playtime.GetPlayTimes(playerSession);

        var totalTime = TimeSpan.Zero;
        foreach (var (jobId, jobTime) in playTimes)
        {
            if (!jobs.Contains(jobId))
                continue;

            totalTime += jobTime;
        }

        return totalTime;
    }

    private void EnsurePrototypesLoaded()
    {
        if (!_jobsDataLoaded)
            ReloadPrototypes();
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        if (args.WasModified<PlayTimeTrackerPrototype>())
            ReloadPrototypes();
    }

    private void ReloadPrototypes()
    {
        var humanJobs = new HashSet<ProtoId<PlayTimeTrackerPrototype>>();
        var xenoJobs = new HashSet<ProtoId<PlayTimeTrackerPrototype>>();

        foreach (var job in _prototype.EnumeratePrototypes<PlayTimeTrackerPrototype>())
        {
            if (job.IsHumanoid)
                humanJobs.Add(job.ID);
            if (job.IsXeno)
                xenoJobs.Add(job.ID);
        }

        _humanJobs = humanJobs.ToFrozenSet();
        _xenoJobs = xenoJobs.ToFrozenSet();

        _jobsDataLoaded = true;
    }
}
