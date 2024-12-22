using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Scaling;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(ScalingSystem))]
public sealed partial class MarineScalingComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Started;

    [DataField, AutoNetworkedField]
    public double Scale;

    [DataField, AutoNetworkedField]
    public double MaxScale;
}
