using Content.Shared.Humanoid;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._CM14.Xenos.Hugger;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class HuggableComponent : Component
{
    [DataField, AutoNetworkedField]
    public Dictionary<Sex, SoundSpecifier> Sound = new()
    {
        [Sex.Male] = new SoundPathSpecifier("/Audio/_CM14/Voice/Human/facehugged_male.ogg"),
        [Sex.Female] = new SoundPathSpecifier("/Audio/_CM14/Voice/Human/facehugged_female.ogg")
    };
}
