using Robust.Shared.GameStates;

namespace Content.Shared._CM14.Doors;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(CMDoorSystem))]
public sealed partial class CMDoubleDoorComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan LastOpeningAt;

    [DataField, AutoNetworkedField]
    public TimeSpan LastClosingAt;
}
