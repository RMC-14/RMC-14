namespace Content.Shared._RMC14.Holiday;

public sealed class RMCHolidaySystem : EntitySystem
{

    private List<string> _holidays = new();

    public void SetHolidays(List<string> holidays)
    {
        _holidays = holidays;
        foreach (var holiday in _holidays)
        {
            Logger.Debug(holiday);
        }
    }

    public List<string> GetActiveHolidays()
    {
        return _holidays;
    }

    public bool IsActiveHoliday(string holidayName)
    {
        return _holidays.Contains(holidayName);
    }
}
