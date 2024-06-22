using DrawDepth = Content.Shared.DrawDepth.DrawDepth;

namespace Content.Client._CM14.Xenonids;

[ByRefEvent]
public record struct GetDrawDepthEvent(DrawDepth DrawDepth);
