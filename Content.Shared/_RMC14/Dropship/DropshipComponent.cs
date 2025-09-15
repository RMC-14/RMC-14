using Content.Shared.Shuttles.Systems;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.Dropship;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(SharedDropshipSystem))]
public sealed partial class DropshipComponent : Component
{
    [DataField, AutoNetworkedField]
    public FTLState State;

    [DataField, AutoNetworkedField]
    public EntityUid? Destination;

    [DataField, AutoNetworkedField]
    public EntityUid? DepartureLocation;

    [DataField, AutoNetworkedField]
    public bool Crashed;

    [DataField, AutoNetworkedField]
    public SoundSpecifier LocalHijackSound = new SoundPathSpecifier("/Audio/_RMC14/Machines/Shuttle/queen_alarm.ogg");

    [DataField, AutoNetworkedField]
    public SoundSpecifier MarineHijackSound = new SoundPathSpecifier("/Audio/_RMC14/Announcements/ARES/hijack.ogg", AudioParams.Default.WithVolume(-5));

    [DataField, AutoNetworkedField]
    public SoundSpecifier GeneralQuartersSound = new SoundPathSpecifier("/Audio/_RMC14/Announcements/ARES/GQfullcall.ogg");

    [DataField, AutoNetworkedField]
    public SoundSpecifier UnidentifledlifesignsSound = new SoundPathSpecifier("/Audio/_RMC14/Announcements/ARES/unidentified_lifesigns.ogg");

    [DataField, AutoNetworkedField]
    public bool Locked;

    [DataField, AutoNetworkedField]
    public TimeSpan LockCooldown = TimeSpan.FromSeconds(1);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan LastLocked;

    [DataField, AutoNetworkedField]
    public HashSet<EntityUid> AttachmentPoints = new();

    [DataField, AutoNetworkedField]
    public TimeSpan? RechargeTime;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan? HijackLandAt;

    [DataField, AutoNetworkedField]
    public EntProtoId FireId = "RMCTileFire";

    [DataField, AutoNetworkedField]
    public int FireRange = 11;

    [DataField, AutoNetworkedField]
    public SoundSpecifier CrashWarningSound = new SoundPathSpecifier("/Audio/_RMC14/Announcements/ARES/dropship_emergency.ogg", AudioParams.Default.WithVolume(-5));

    [DataField, AutoNetworkedField]
    public SoundSpecifier CrashSound = new SoundPathSpecifier("/Audio/_RMC14/Dropship/dropship_crash.ogg", AudioParams.Default.WithVolume(-1));

    [DataField, AutoNetworkedField]
    public SoundSpecifier IncomingSound = new SoundPathSpecifier("/Audio/_RMC14/Dropship/dropship_incoming.ogg", AudioParams.Default.WithVolume(-1));

    [DataField, AutoNetworkedField]
    public bool AnnouncedCrash;

    [DataField, AutoNetworkedField]
    public bool DidIncomingSound;

    [DataField, AutoNetworkedField]
    public bool DidExplosion;

    [DataField, AutoNetworkedField]
    public TimeSpan AnnounceCrashTime = TimeSpan.FromSeconds(23);

    [DataField, AutoNetworkedField]
    public TimeSpan PlayIncomingSoundTime = TimeSpan.FromSeconds(10);

    [DataField, AutoNetworkedField]
    public TimeSpan ExplodeTime = TimeSpan.FromSeconds(3);

    [DataField, AutoNetworkedField]
    public TimeSpan CancelFlightTime = TimeSpan.FromSeconds(10);
}
