using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.Tether;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCTetherComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool VisibleToOrigin = true;

    [DataField, AutoNetworkedField]
    public MapCoordinates? StaticTetherOrigin;

    [DataField, AutoNetworkedField]
    public EntityUid? TetherOrigin;

    [DataField]
    public ResPath RsiPath = new("/Textures/_RMC14/Effects/beam.rsi");

    [DataField]
    public string TetherState = "oppressor_tail";

    [DataField]
    public float TetherWidth = 1;
}
