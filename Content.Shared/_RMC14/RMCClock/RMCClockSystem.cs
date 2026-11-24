using System.Linq;
using Content.Shared._RMC14.UniformAccessories;
using Content.Shared.Clock;
using Content.Shared.Examine;
using Content.Shared.GameTicking;

namespace Content.Shared._RMC14.RMCClock;

public sealed class RMCClockSystem : EntitySystem
{
    [Dependency] private readonly SharedGameTicker _ticker = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCClockComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<RMCClockComponent, AccessoryRelayedEvent<ExaminedEvent>>(OnEquipedExamined);
    }

    public DateTime GetWorldTime()
    {
        var globalTime = EntityQuery<GlobalTimeManagerComponent>().FirstOrDefault();
        var worldTime = (globalTime?.TimeOffset ?? TimeSpan.Zero) + _ticker.RoundDuration();
        return (globalTime?.DateOffset ?? DateTime.Today.AddYears(100)) + worldTime;
    }

    private string GetFormattedTime()
    {
        return GetWorldTime().ToString("dd MMMM, yyyy - HH:mm");
    }

    private void OnExamined(Entity<RMCClockComponent> ent, ref ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("rmc-clock-examine", ("device", ent.Owner), ("time", GetFormattedTime())));
    }

    private void OnEquipedExamined(Entity<RMCClockComponent> ent, ref AccessoryRelayedEvent<ExaminedEvent> args)
    {
        args.Args.PushMarkup(Loc.GetString("rmc-clock-examine", ("device", ent.Owner), ("time", GetFormattedTime())));
    }
}
