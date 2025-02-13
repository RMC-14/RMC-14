using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Telephone;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedRMCTelephoneSystem))]
public sealed partial class RMCTelephoneComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? RotaryPhone;

    [DataField, AutoNetworkedField]
    public SoundSpecifier SpeakSound = new SoundCollectionSpecifier("RMCPhoneSpeak", AudioParams.Default.WithVolume(-3));
}
