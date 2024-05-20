using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._CM14.Pulling;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ParalyzeOnPullAttemptComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan Duration = TimeSpan.FromSeconds(8);

    [DataField, AutoNetworkedField]
    public SoundSpecifier? Sound = new SoundPathSpecifier("/Audio/_CM14/Weapons/pierce.ogg", AudioParams.Default.WithVolume(-10));

    [DataField, AutoNetworkedField]
    public float MinPitch = 3;

    [DataField, AutoNetworkedField]
    public float MaxPitch = 4;
}
