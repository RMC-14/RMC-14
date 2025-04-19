namespace Content.Shared._RMC14.Holiday;

public abstract class SharedRMCHolidaySystem : EntitySystem
{

    public List<string> GetActiveHolidays()
    {
        var query = EntityQueryEnumerator<RMCHolidayTrackerComponent>();
        while (query.MoveNext(out var holidayTracker))
        {
            return holidayTracker.ActiveHolidays;
        }

        return [];
    }

    public bool IsActiveHoliday(string holidayName)
    {
        return GetActiveHolidays().Contains(holidayName);
    }
}
