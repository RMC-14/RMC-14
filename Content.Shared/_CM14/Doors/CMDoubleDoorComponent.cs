using Robust.Shared.GameStates;
using Robust.Shared.Timing;

namespace Content.Shared._CM14.Doors;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CMDoubleDoorComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan LastOpeningAt;

    [DataField, AutoNetworkedField]
    public TimeSpan LastClosingAt;
}
