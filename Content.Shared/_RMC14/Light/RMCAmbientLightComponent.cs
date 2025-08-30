using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Light;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCAmbientLightComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool IsAnimating;

    [DataField, AutoNetworkedField]
    public TimeSpan Duration;

    [DataField, AutoNetworkedField]
    public TimeSpan StartTime = TimeSpan.Zero;

    [ViewVariables]
    public TimeSpan EndTime => StartTime + Duration;

    [DataField, AutoNetworkedField]
    public List<Color> Colors = new();
}
