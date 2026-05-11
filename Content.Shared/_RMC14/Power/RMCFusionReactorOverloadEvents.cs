using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Power;

[ByRefEvent]
public sealed class RMCFusionReactorCanOverloadEvent(EntityUid reactor, EntityUid user) : EntityEventArgs
{
    public readonly EntityUid Reactor = reactor;
    public readonly EntityUid User = user;
    public bool CanOverload;
}

[ByRefEvent]
public sealed class RMCFusionReactorOverloadStatusEvent(EntityUid reactor, EntityUid examiner) : EntityEventArgs
{
    public readonly EntityUid Reactor = reactor;
    public readonly EntityUid Examiner = examiner;
    public string? Text;
}

[ByRefEvent]
public readonly record struct RMCFusionReactorOverloadChangedEvent(EntityUid Reactor, bool Overloaded);
