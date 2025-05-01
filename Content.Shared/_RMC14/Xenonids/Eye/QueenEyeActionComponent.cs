using Content.Shared.Eye;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Xenonids.Eye;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(QueenEyeSystem))]
public sealed partial class QueenEyeActionComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntProtoId Spawn = "RMCQueenEye";

    [DataField, AutoNetworkedField]
    public VisibilityFlags Visibility = VisibilityFlags.Xeno;

    [DataField, AutoNetworkedField]
    public float PvsScale = 1;

    [DataField, AutoNetworkedField]
    public float EyePvsScale = 1.5f;

    [DataField, AutoNetworkedField]
    public EntityUid? Eye;
}
