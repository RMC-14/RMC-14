using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Holiday;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedRMCHolidaySystem))]
public sealed partial class RMCHolidayTrackerComponent : Component
{
    [DataField, AutoNetworkedField]
    public List<string> ActiveHolidays = new();
}
