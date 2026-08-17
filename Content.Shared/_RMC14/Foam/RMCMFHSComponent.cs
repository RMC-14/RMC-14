using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Foam;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCMFHSComponent : Component
{
    [DataField, AutoNetworkedField]
    public int Range = 2;

    [DataField, AutoNetworkedField]
    public TimeSpan StunTime = TimeSpan.FromSeconds(0.3);

    [DataField, AutoNetworkedField]
    public float KnockbackSpeed = 10f;

    [DataField, AutoNetworkedField]
    public TimeSpan SpreadDelay = TimeSpan.FromSeconds(0.1);

    [DataField, AutoNetworkedField]
    public SoundSpecifier DeploySound = new SoundPathSpecifier("/Audio/_RMC14/Items/fulton.ogg");

    [DataField, AutoNetworkedField]
    public EntProtoId Foam = "RMCMFHSFoam";
}
