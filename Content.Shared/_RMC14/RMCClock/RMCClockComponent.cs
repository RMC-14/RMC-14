using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.RMCClock;

[RegisterComponent, NetworkedComponent]
[Access(typeof(RMCClockSystem))]
[AutoGenerateComponentState]
public sealed partial class RMCClockComponent : Component
{
    /// <summary>
    /// If not null, this time will be permanently shown.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan? StuckTime;

    /// <summary>
    /// The format in which time is displayed.
    /// </summary>
    [DataField, AutoNetworkedField]
    public RMCClockType ClockType = RMCClockType.TwelveHour;

    [DataField]
    public string HoursBase = "hours_";

    [DataField]
    public string MinutesBase = "minutes_";
}

[Serializable, NetSerializable]
public enum RMCClockType : byte
{
    TwelveHour,
    TwentyFourHour
}
