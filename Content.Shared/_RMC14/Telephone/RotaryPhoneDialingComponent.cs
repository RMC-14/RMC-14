using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.Telephone;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(SharedTelephoneSystem))]
public sealed partial class RotaryPhoneDialingComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? Other;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan LastVoicemail;

    [DataField, AutoNetworkedField]
    public bool DidVoicemail = false;

    [DataField, AutoNetworkedField]
    public bool DidVoicemailTimeout = false;
}
