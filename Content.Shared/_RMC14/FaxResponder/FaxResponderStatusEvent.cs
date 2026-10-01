using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Content.Shared.Roles;

namespace Content.Shared._RMC14.FaxResponder;

[Serializable, NetSerializable]
public readonly record struct FaxResponderJobStatus(ProtoId<JobPrototype> Job, bool Taken);

[Serializable, NetSerializable]
public sealed class FaxResponderStatusEvent(List<FaxResponderJobStatus> jobs) : EntityEventArgs
{
    public List<FaxResponderJobStatus> Jobs { get; } = jobs;
}
