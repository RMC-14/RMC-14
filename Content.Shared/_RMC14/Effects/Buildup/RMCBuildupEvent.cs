using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Effects.Buildup;

[ByRefEvent]
public record struct RMCBuildupTriggeredEvent(
    EntityUid Target,
    ProtoId<RMCBuildupPrototype> Buildup,
    EntityUid? User);

public enum RMCBuildupApplyResult : byte
{
    None,
    Applied,
    Started,
    Triggered,
}
