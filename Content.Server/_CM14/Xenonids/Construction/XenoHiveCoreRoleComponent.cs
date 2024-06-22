namespace Content.Server._CM14.Xenonids.Construction;

[RegisterComponent]
[Access(typeof(XenoHiveCoreSystem))]
public sealed partial class XenoHiveCoreRoleComponent : Component
{
    [DataField]
    public EntityUid? Core;
}
