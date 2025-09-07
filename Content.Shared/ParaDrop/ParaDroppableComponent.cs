using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.ParaDrop;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ParaDroppableComponent : Component
{
    [DataField, AutoNetworkedField]
    public float DropDuration = 4f;

    [DataField, AutoNetworkedField]
    public int DropScatter = 7;

    [DataField, AutoNetworkedField]
    public float FallHeight = 16;

    [DataField, AutoNetworkedField]
    public SoundSpecifier DropSound = new SoundPathSpecifier("/Audio/_RMC14/Items/fulton.ogg");

    [DataField, AutoNetworkedField]
    public EntProtoId ParachutePrototype = "RMCParachuteDeployed";

    [DataField, AutoNetworkedField]
    public TimeSpan? LastParaDrop;
}
