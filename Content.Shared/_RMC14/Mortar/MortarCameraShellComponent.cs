using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Mortar;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedMortarSystem))]
public sealed partial class MortarCameraShellComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan Duration = TimeSpan.FromMinutes(3);

    [DataField, AutoNetworkedField]
    public SoundSpecifier? Sound = new SoundPathSpecifier("/Audio/Weapons/Guns/Gunshots/flaregun.ogg");

    [DataField, AutoNetworkedField]
    public EntProtoId Flare = "RMCMortarFlare";

    [DataField, AutoNetworkedField]
    public EntProtoId Camera = "RMCMortarCamera";
}
