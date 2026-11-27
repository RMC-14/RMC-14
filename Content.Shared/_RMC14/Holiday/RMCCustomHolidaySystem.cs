using System.Linq;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.RMCCustomHoliday;

public sealed partial class RMCCustomHolidaySystem : EntitySystem
{
    [Dependency] private IPrototypeManager _prototypeManager = default!;

    public IEnumerable<CustomHolidayPrototype> GetCustomHolidays()
    {
        return _prototypeManager.EnumeratePrototypes<CustomHolidayPrototype>();
    }
}
