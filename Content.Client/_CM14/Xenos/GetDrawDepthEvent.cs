using DrawDepth = Content.Shared.DrawDepth.DrawDepth;

namespace Content.Client._CM14.Xenos;

[ByRefEvent]
public record struct GetDrawDepthEvent(DrawDepth DrawDepth);
