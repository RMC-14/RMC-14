using Robust.Shared.Audio;

namespace Content.Shared._RMC14.TacticalMap;

[RegisterComponent]
[Access(typeof(TacticalMapAnnounceSystem))]
public sealed partial class TacticalMapMarineAnnounceComponent : Component
{
    [DataField(required: true)]
    public string Message = string.Empty;
}

[RegisterComponent]
[Access(typeof(TacticalMapAnnounceSystem))]
public sealed partial class TacticalMapXenoAnnounceComponent : Component
{
    [DataField(required: true)]
    public string Message = string.Empty;

    [DataField]
    public SoundSpecifier? Sound = new SoundCollectionSpecifier("XenoQueenCommand", AudioParams.Default.WithVolume(-6));
}
