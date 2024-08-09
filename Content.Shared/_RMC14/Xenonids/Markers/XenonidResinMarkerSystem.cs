namespace Content.Shared._RMC14.Xenonids.Markers;

public sealed class XenonidResinMarkerSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
    }
    // Can't be placed in walls
    // Can't be placed out of FOV. Needs line of sight.
    // Can be placed anywhere in visible range.
}
