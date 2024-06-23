namespace Content.Server._RMC14.Xenonids.Watch;

[RegisterComponent]
[Access(typeof(XenoWatchSystem))]
public sealed partial class XenoWatchedComponent : Component
{
    [DataField]
    public HashSet<EntityUid> Watching = new();
}
