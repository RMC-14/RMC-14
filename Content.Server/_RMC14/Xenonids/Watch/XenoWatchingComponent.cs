namespace Content.Server._RMC14.Xenonids.Watch;

[RegisterComponent]
[Access(typeof(XenoWatchSystem))]
public sealed partial class XenoWatchingComponent : Component
{
    public EntityUid? Watching;
}
