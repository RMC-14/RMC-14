namespace Content.Shared._RMC14.Xenonids.Watch;

[RegisterComponent]
[Access(typeof(SharedWatchXenoSystem))]
public sealed partial class XenoWatchingComponent : Component
{
    public EntityUid? Watching;
}
