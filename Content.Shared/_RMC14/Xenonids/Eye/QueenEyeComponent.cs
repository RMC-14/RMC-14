using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.Eye;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(QueenEyeSystem))]
public sealed partial class QueenEyeComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? Queen;

    /// <summary>
    /// Maximum distance in tiles the queen eye can move from the nearest weed.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float MaxWeedDistance = 3f;
}
