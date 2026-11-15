using JetBrains.Annotations;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.NewPlayer;

/// <summary>
///     Event raised when a player has 1 or less hours in a job.
/// </summary>
[Serializable, NetSerializable]
public sealed class NewToJobEvent : EntityEventArgs
{
    public NetEntity Mob { get; }
    public string? JobInfo { get; }
    public string JobName { get; }

    public NewToJobEvent(NetEntity mob, string? jobInfo, string jobName)
    {
        Mob = mob;
        JobInfo = jobInfo;
        JobName = jobName;
    }
}
