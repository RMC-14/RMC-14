using Robust.Shared.GameStates;

namespace Content.Shared._CM14.Pulling;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(CMPullingSystem))]
public sealed partial class SlowOnPullComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public float Multiplier = 1;
}
