namespace Content.Server._CM14.Xenos.Watch;

[RegisterComponent]
[Access(typeof(XenoWatchSystem))]
public sealed partial class XenoWatchingComponent : Component
{
    public EntityUid? Watching;
}
