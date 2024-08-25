using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.Bioscan;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(BioscanSystem))]
public sealed partial class BioscanComponent : Component
{
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan NextCheck;

    [DataField, AutoNetworkedField]
    public int MaxMarinesAlive;

    [DataField, AutoNetworkedField]
    public int MaxXenoAlive;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan LastMarine;

    [DataField, AutoNetworkedField]
    public SoundSpecifier MarineSound = new SoundPathSpecifier("/Audio/_RMC14/Announcements/ARES/bioscan.ogg", AudioParams.Default.WithVolume(-6));

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan LastXeno;

    [DataField, AutoNetworkedField]
    public SoundSpecifier XenoSound = new SoundCollectionSpecifier("XenoQueenCommand", AudioParams.Default.WithVolume(-6));
}
