using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Light;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCLightAnimationComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan Duration;

    [DataField, AutoNetworkedField]
    public List<string> ColorHexes;

    [DataField, AutoNetworkedField]
    public float StepPercent;

    [DataField, AutoNetworkedField]
    public Color PreviousColor;

    [DataField, AutoNetworkedField]
    public Color NextColor;
}
