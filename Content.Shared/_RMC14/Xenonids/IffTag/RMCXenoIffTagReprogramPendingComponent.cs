namespace Content.Shared._RMC14.Xenonids.IffTag;

[RegisterComponent, Access(typeof(RMCXenoIffTagSystem))]
public sealed partial class RMCXenoIffTagReprogramPendingComponent : Component
{
    [DataField]
    public EntityUid Programmer;
}
