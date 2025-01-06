using System.Linq;
using Content.Server.GameTicking.Events;
using Content.Server.Holiday;
using Content.Shared._RMC14.Holiday;
using Robust.Server.GameStates;

namespace Content.Server._RMC14.Holiday;

public sealed class RMCHolidaySystem : SharedRMCHolidaySystem
{

    [Dependency] private readonly HolidaySystem _holiday = default!;
    [Dependency] private readonly PvsOverrideSystem _pvsOverride = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundStartingEvent>(OnRoundStart);
        SubscribeLocalEvent<RMCHolidayTrackerComponent, MapInitEvent>(OnMapInit);
    }

    private void OnRoundStart(RoundStartingEvent ev)
    {
        var manager = Spawn();
        AddComp<RMCHolidayTrackerComponent>(manager);
    }

    private void OnMapInit(Entity<RMCHolidayTrackerComponent> ent, ref MapInitEvent ev)
    {
        ent.Comp.ActiveHolidays = _holiday.GetCurrentHolidays().Select(x => x.ID).ToList();
        _pvsOverride.AddGlobalOverride(ent);
        Dirty(ent);
    }
}
