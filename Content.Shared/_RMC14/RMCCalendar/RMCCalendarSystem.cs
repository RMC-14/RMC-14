using System.Linq;
using Robust.Shared.Prototypes;
using Content.Shared.Clock;
using Content.Shared.Examine;
using Content.Shared.GameTicking;
using Content.Shared._RMC14.RMCCustomHoliday;

namespace Content.Shared._RMC14.RMCCalendar;

public sealed class RMCCalendarSystem : EntitySystem
{
    [Dependency] private readonly SharedGameTicker _ticker = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly RMCCustomHolidaySystem _customHolidaySystem = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCCalendarComponent, ExaminedEvent>(OnExamined);
    }

    private void OnExamined(Entity<RMCCalendarComponent> ent, ref ExaminedEvent args)
    {
        var owner = ent.Owner;
        var worldDate = EntityQuery<GlobalTimeManagerComponent>().FirstOrDefault()?.DateOffset ?? DateTime.Today.AddYears(100);
        var time = worldDate.ToString("dd MMMM, yyyy");

        var todayHolidays = _customHolidaySystem.GetCustomHolidays()
            .Where(h => h.BeginDay == worldDate.Day && h.BeginMonth.Equals(worldDate.ToString("MMMM"), StringComparison.OrdinalIgnoreCase))
            .ToList();

        foreach (var holiday in todayHolidays)
        {
            args.PushMarkup(Loc.GetString("rmc-calendar-holiday-examine", ("holidayname", holiday.Name), ("holidaydescription", holiday.Description)));
        }

        args.PushMarkup(Loc.GetString("rmc-calendar-examine", ("time", time)));
    }
}
