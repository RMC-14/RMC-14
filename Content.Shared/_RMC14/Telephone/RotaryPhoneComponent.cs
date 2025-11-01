using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.Telephone;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(SharedRMCTelephoneSystem))]
public sealed partial class RotaryPhoneComponent : Component
{
    [DataField, AutoNetworkedField]
    public string Category = "Almayer";

    [DataField, AutoNetworkedField]
    public bool CanDnd;

    [DataField, AutoNetworkedField]
    public SoundSpecifier? DialingSound = new SoundPathSpecifier("/Audio/_RMC14/Phone/dial.ogg", AudioParams.Default.WithVolume(-3));

    [DataField, AutoNetworkedField]
    public SoundSpecifier? DialingIdleSound = new SoundPathSpecifier("/Audio/_RMC14/Phone/ring_outgoing.ogg", AudioParams.Default.WithVolume(-3));

    [DataField, AutoNetworkedField]
    public SoundSpecifier? ReceivingSound = new SoundPathSpecifier("/Audio/_RMC14/Phone/telephone_ring.ogg", AudioParams.Default.WithVolume(-3));

    [DataField, AutoNetworkedField]
    public SoundSpecifier? GrabSound = new SoundCollectionSpecifier("RMCRadioTelephoneGrab");

    [DataField, AutoNetworkedField]
    public SoundSpecifier? VoicemailSound = new SoundPathSpecifier("/Audio/_RMC14/Machines/Phone/voicemail.ogg", AudioParams.Default.WithVolume(-3));

    [DataField, AutoNetworkedField]
    public EntProtoId<RMCTelephoneComponent> PhoneId = "RMCTelephone";

    [DataField, AutoNetworkedField]
    public string ContainerId = "rmc_rotary_phone_telephone";

    [DataField, AutoNetworkedField]
    public EntityUid? Phone;

    [DataField, AutoNetworkedField]
    public EntityUid? Sound;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan LastCall;

    [DataField, AutoNetworkedField]
    public TimeSpan CallCooldown = TimeSpan.FromSeconds(1);

    [DataField, AutoNetworkedField]
    public TimeSpan DialingIdleDelay = TimeSpan.FromSeconds(3);

    [DataField, AutoNetworkedField]
    public TimeSpan VoicemailDelay = TimeSpan.FromSeconds(30);

    [DataField, AutoNetworkedField]
    public TimeSpan VoicemailTimeoutDelay = TimeSpan.FromSeconds(6);

    [DataField]
    public EntityUid? VoicemailSoundEntity;

    [DataField, AutoNetworkedField]
    public bool Idle;

    [DataField, AutoNetworkedField]
    public bool TryGetHolderName = true;

    /// <summary>
    /// Should admins be notified when being called.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField]
    public bool NotifyAdmins { get; set; } = false;
}

[Serializable, NetSerializable]
public enum RotaryPhoneLayers
{
    Layer,
}

[Serializable, NetSerializable]
public enum RotaryPhoneVisuals
{
    Base,
    Ring,
    Ear,
}
