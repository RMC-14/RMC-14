using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.TacticalMap;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(TacticalMapSystem))]
public sealed partial class MinimapColorComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public Color Color;
}
