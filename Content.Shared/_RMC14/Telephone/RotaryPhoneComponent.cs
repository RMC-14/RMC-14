using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.Telephone;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(SharedTelephoneSystem))]
public sealed partial class RotaryPhoneComponent : Component
{
    [DataField, AutoNetworkedField]
    public string Category = "Almayer";

    [DataField, AutoNetworkedField]
    public bool CanDnd;

    [DataField, AutoNetworkedField]
    public SoundSpecifier? DialingSound = new SoundPathSpecifier("/Audio/_RMC14/Phone/dial.ogg");

    [DataField, AutoNetworkedField]
    public SoundSpecifier? DialingIdleSound = new SoundPathSpecifier("/Audio/_RMC14/Phone/ring_outgoing.ogg");

    [DataField, AutoNetworkedField]
    public SoundSpecifier? ReceivingSound = new SoundPathSpecifier("/Audio/_RMC14/Phone/telephone_ring.ogg");

    [DataField, AutoNetworkedField]
    public EntProtoId<TelephoneComponent> PhoneId = "RMCTelephone";

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
    public bool Idle;
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
