using System.Diagnostics.CodeAnalysis;
using Content.Shared._RMC14.Preferences;
using Content.Shared.Players.PlayTimeTracking;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;

// ReSharper disable CheckNamespace
namespace Content.Shared.Preferences;
// ReSharper restore CheckNamespace

public sealed partial record PlayerPreferences
{
    private readonly Dictionary<ProtoId<JobPrototype>, JobPriority> _jobPriorities = new();

    public IReadOnlyDictionary<ProtoId<JobPrototype>, JobPriority> JobPriorities
    {
        get => _jobPriorities;
        init => _jobPriorities = SanitizeJobPriorities(value);
    }

    public RMCPlayerSettings PlayerSettings { get; init; } = RMCPlayerSettings.Default;

    public bool TryGetHumanoidInSlot(int slot, [NotNullWhen(true)] out HumanoidCharacterProfile? humanoid)
    {
        humanoid = null;
        if (!Characters.TryGetValue(slot, out var profile))
            return false;

        humanoid = profile as HumanoidCharacterProfile;
        return humanoid != null;
    }

    public IEnumerable<(int Slot, HumanoidCharacterProfile Profile)> GetCharactersForJob(
        ProtoId<JobPrototype> job,
        bool includeDisabled = false)
    {
        foreach (var (slot, profile) in Characters)
        {
            if (profile is not HumanoidCharacterProfile humanoid)
                continue;

            if (!includeDisabled && !humanoid.Enabled)
                continue;

            if (!humanoid.JobPreferences.Contains(job))
                continue;

            yield return (slot, humanoid);
        }
    }

    public bool HasCharacterForJob(ProtoId<JobPrototype> job)
    {
        foreach (var _ in GetCharactersForJob(job))
        {
            return true;
        }

        return false;
    }

    public Dictionary<ProtoId<JobPrototype>, JobPriority> GetPlayableJobPriorities(IPrototypeManager prototypes)
    {
        var playable = new Dictionary<ProtoId<JobPrototype>, JobPriority>();
        foreach (var (job, priority) in _jobPriorities)
        {
            if (IsCharacterlessJob(job, prototypes) || HasCharacterForJob(job))
                playable[job] = priority;
        }

        return playable;
    }

    public static bool IsCharacterlessJob(ProtoId<JobPrototype> job, IPrototypeManager prototypes)
    {
        return prototypes.TryIndex(job, out var jobPrototype) &&
               prototypes.TryIndex<PlayTimeTrackerPrototype>(jobPrototype.PlayTimeTracker, out var tracker) &&
               tracker.IsXeno;
    }

    public static Dictionary<ProtoId<JobPrototype>, JobPriority> SanitizeJobPriorities(
        IEnumerable<KeyValuePair<ProtoId<JobPrototype>, JobPriority>> priorities)
    {
        var sanitized = new Dictionary<ProtoId<JobPrototype>, JobPriority>();
        var hasHigh = false;
        foreach (var (job, priority) in priorities)
        {
            switch (priority)
            {
                case JobPriority.Low:
                case JobPriority.Medium:
                    sanitized[job] = priority;
                    break;
                case JobPriority.High:
                    sanitized[job] = hasHigh ? JobPriority.Medium : JobPriority.High;
                    hasHigh = true;
                    break;
            }
        }

        return sanitized;
    }
}
