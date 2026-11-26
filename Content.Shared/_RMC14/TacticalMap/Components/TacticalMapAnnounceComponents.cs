using Content.Shared._RMC14.Announce;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.TacticalMap;

[RegisterComponent]
[Access(typeof(TacticalMapAnnounceSystem))]
public sealed partial class TacticalMapMarineAnnounceComponent : Component
{
    [DataField(required: true)]
    public string Message = string.Empty;

    [DataField]
    public EntProtoId<RMCAnnouncementPresetComponent>? Overlay = "RMCAnnouncementMarineCommandNoPortrait";
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
