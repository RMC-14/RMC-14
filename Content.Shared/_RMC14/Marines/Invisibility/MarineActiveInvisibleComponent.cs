using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Marines.Invisibility;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class MarineActiveInvisibleComponent : Component
{
    [DataField, AutoNetworkedField]
    public float Opacity = 0.1f;
}
