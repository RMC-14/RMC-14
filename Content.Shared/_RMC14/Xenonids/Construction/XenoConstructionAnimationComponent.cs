using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.Construction;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class XenoConstructionAnimationComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan AnimationTimeFinished = TimeSpan.FromSeconds(1);

    [DataField, AutoNetworkedField]
    public TimeSpan AnimationTime = TimeSpan.FromSeconds(1);

    [DataField, AutoNetworkedField]
    public int TotalFrames = 0;

}
