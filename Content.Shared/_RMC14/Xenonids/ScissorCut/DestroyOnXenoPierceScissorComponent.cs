using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Xenonids.ScissorCut;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class DestroyOnXenoPierceScissorComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntProtoId SpawnPrototype;

    [DataField, AutoNetworkedField]
    public SoundSpecifier Sound;
}
