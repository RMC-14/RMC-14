namespace Content.Server._RMC14.Xenonids.Construction;

[RegisterComponent]
[Access(typeof(XenoHiveCoreSystem))]
public sealed partial class XenoHiveCoreRoleComponent : Component
{
    [DataField]
    public EntityUid? Core;
}
