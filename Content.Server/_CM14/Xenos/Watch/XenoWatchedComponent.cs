namespace Content.Server._CM14.Xenos.Watch;

[RegisterComponent]
[Access(typeof(XenoWatchSystem))]
public sealed partial class XenoWatchedComponent : Component
{
    [DataField]
    public HashSet<EntityUid> Watching = new();
}
