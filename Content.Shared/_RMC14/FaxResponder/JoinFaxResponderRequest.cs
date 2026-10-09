using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Content.Shared.Roles;

namespace Content.Shared._RMC14.FaxResponder;

[Serializable, NetSerializable]
public sealed class JoinFaxResponderRequest(ProtoId<JobPrototype> job) : EntityEventArgs
{
    public ProtoId<JobPrototype> Job { get; } = job;
}
