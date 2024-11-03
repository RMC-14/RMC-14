using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Telephone;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedTelephoneSystem))]
public sealed partial class TelephoneComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? RotaryPhone;

    [DataField, AutoNetworkedField]
    public SoundSpecifier SpeakSound = new SoundCollectionSpecifier("RMCPhoneSpeak");
}
