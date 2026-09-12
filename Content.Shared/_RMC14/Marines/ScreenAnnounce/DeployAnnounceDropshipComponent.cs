using Content.Shared.Shuttles.Systems;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.Marines.ScreenDeployAnnounce;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedDeployAnnounceSystem))]
public sealed partial class DeployAnnounceDropshipComponent : Component
{
    [DataField, AutoNetworkedField]
    public LocId AnnounceText = "deploy-combat-alert";

    [DataField, AutoNetworkedField]
    public SoundSpecifier? AfterAnnounceSound;

    [DataField, AutoNetworkedField]
    public bool Deployed = false;
}

