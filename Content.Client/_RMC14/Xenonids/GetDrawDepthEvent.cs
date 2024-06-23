using DrawDepth = Content.Shared.DrawDepth.DrawDepth;

namespace Content.Client._RMC14.Xenonids;

[ByRefEvent]
public record struct GetDrawDepthEvent(DrawDepth DrawDepth);
