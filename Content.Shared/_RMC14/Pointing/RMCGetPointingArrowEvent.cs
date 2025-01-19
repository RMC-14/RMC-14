using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Pointing;

[ByRefEvent]
public record struct RMCGetPointingArrowEvent(EntProtoId Arrow);
