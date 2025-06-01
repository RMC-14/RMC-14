using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.Eye;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(QueenEyeSystem))]
public sealed partial class QueenEyeVisionComponent : Component
{
    /// <summary>
    ///     Range in tiles
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Range = 28;
}
