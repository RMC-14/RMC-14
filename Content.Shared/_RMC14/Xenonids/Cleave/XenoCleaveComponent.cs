using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Xenonids.Cleave;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class XenoCleaveComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan RootTime = TimeSpan.FromSeconds(1);

    [DataField, AutoNetworkedField]
    public TimeSpan RootTimeBuffed = TimeSpan.FromSeconds(1.8);

    [DataField, AutoNetworkedField]
    public float FlingDistance = 3f;

    [DataField, AutoNetworkedField]
    public float FlingDistanceBuffed = 6f;

    [DataField, AutoNetworkedField]
    public SoundSpecifier Sound = new SoundCollectionSpecifier("Punch");

    [DataField, AutoNetworkedField]
    public EntProtoId RootEffect = "CMEffectPunch";

    [DataField, AutoNetworkedField]
    public EntProtoId FlingEffect = "RMCEffectSlam";
}
