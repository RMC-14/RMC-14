using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared._CM14.Xenos.Hive;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class HiveComponent : Component
{
    [DataField, AutoNetworkedField]
    public Dictionary<int, FixedPoint2> TierLimits = new()
    {
        [2] = 0.5,
        [3] = 0.2
    };
}
