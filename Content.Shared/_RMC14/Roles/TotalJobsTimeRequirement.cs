using System.Diagnostics.CodeAnalysis;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.Roles;

[Serializable, NetSerializable]
public sealed partial class TotalJobsTimeRequirement : JobRequirement
{
    /// <summary>
    /// Which roles to add up to the required amount of time.
    /// </summary>
    [DataField(required: true)]
    public EntProtoId Group;

    /// <summary>
    /// How long (in seconds) this requirement is.
    /// </summary>
    [DataField(required: true)]
    public TimeSpan Time;

    public bool TryRequirementsMet(IReadOnlyDictionary<string, TimeSpan> playTimes, out FormattedMessage? reason, IEntityManager entManager, IPrototypeManager prototypes)
    {
        reason = null;
        var playtime = TimeSpan.Zero;
        var trackers = new HashSet<string>();
        if (!prototypes.Index(Group).TryGetComponent(out JobGroupComponent? comp))
        {
            var sawmill = Logger.GetSawmill("job.requirements");
            sawmill.Error($"No {nameof(DepartmentGroupComponent)} found on entity {Group}");
            return true;
        }

        // Check all jobs' playtime
        foreach (var jobId in comp.Jobs)
        {
            // The schema is stored on the Job role but we want to explode if the timer isn't found anyway.
            var proto = prototypes.Index(jobId).PlayTimeTracker;
            trackers.Add(proto);
        }

        foreach (var tracker in trackers)
        {
            playTimes.TryGetValue(tracker, out var otherTime);
            playtime += otherTime;
        }

        var deptDiff = Time.TotalMinutes - playtime.TotalMinutes;

        if (!Inverted)
        {
            if (deptDiff <= 0)
                return true;

            reason = FormattedMessage.FromMarkupOrThrow(Loc.GetString(
                "role-timer-total-department-insufficient",
                ("time", Math.Ceiling(deptDiff)),
                ("roles", Loc.GetString(comp.Name)),
                ("rolesColor", comp.Color.ToHex())));
            return false;
        }
        else
        {
            if (deptDiff <= 0)
            {
                reason = FormattedMessage.FromMarkupOrThrow(Loc.GetString(
                    "role-timer-total-department-too-high",
                    ("time", -deptDiff),
                    ("roles", Loc.GetString(comp.Name)),
                    ("rolesColor", comp.Color.ToHex())));
                return false;
            }

            return true;
        }
    }

    public override bool Check(IEntityManager entManager,
        IPrototypeManager protoManager,
        HumanoidCharacterProfile? profile,
        IReadOnlyDictionary<string, TimeSpan> playTimes,
        [NotNullWhen(false)] out FormattedMessage? reason)
    {
        return TryRequirementsMet(playTimes, out reason, entManager, protoManager);
    }
}
