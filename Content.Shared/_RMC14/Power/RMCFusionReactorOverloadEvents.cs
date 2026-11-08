using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Power;

[ByRefEvent]
public record struct RMCFusionReactorCanOverloadEvent(EntityUid Reactor, EntityUid User)
{
    public bool CanOverload;
}

[ByRefEvent]
public record struct RMCFusionReactorOverloadStatusEvent(EntityUid Reactor, EntityUid Examiner)
{
    public string? Text;
}

[ByRefEvent]
public readonly record struct RMCFusionReactorOverloadChangedEvent(EntityUid Reactor, bool Overloaded);
