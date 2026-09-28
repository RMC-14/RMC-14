namespace Content.Shared._RMC14.Hijack;

[RegisterComponent]
public sealed partial class RMCHijackActivePipeComponent : Component
{
    [DataField]
    public EntityUid? Map;
}
