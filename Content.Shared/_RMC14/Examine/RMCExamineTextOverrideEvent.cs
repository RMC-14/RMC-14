using Robust.Shared.Utility;

namespace Content.Shared._RMC14.Examine;

[ByRefEvent]
public record struct RMCExamineTextOverrideEvent(EntityUid Examined, FormattedMessage? Message = null);
