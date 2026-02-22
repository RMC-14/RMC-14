using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.Eye;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(QueenEyeSystem))]
public sealed partial class QueenEyeComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? Queen;

    [DataField, AutoNetworkedField]
    public float MaxWeedDistance = 3.5f;

    [DataField, AutoNetworkedField]
    public float SoftWeedDistance = 3f;
}
