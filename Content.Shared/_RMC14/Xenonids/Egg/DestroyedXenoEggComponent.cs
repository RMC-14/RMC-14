using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.Egg;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class DestroyedXenoEggComponent : Component
{
    [DataField, AutoNetworkedField]
    public string AnimationState = "egg_exploding";

    [DataField, AutoNetworkedField]
    public TimeSpan AnimationTime = TimeSpan.FromSeconds(0.7);

    [DataField, AutoNetworkedField]
    public string Layer = "egg";
}
