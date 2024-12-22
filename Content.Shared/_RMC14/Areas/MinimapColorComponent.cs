using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Areas;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(AreaSystem))]
public sealed partial class MinimapColorComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public Color Color;
}
