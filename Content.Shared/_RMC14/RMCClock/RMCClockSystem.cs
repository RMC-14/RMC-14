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

    private string GetTime()
    {
        var worldTime = (EntityQuery<GlobalTimeManagerComponent>().FirstOrDefault()?.TimeOffset ?? TimeSpan.Zero) + _ticker.RoundDuration();
        var worldDate = (EntityQuery<GlobalTimeManagerComponent>().FirstOrDefault()?.DateOffset ?? DateTime.Today.AddYears(100))
                        + worldTime;

        return worldDate.ToString("dd MMMM, yyyy - HH:mm");
    }

    private void OnExamined(Entity<RMCClockComponent> ent, ref ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("rmc-clock-examine", ("device", ent.Owner), ("time", GetTime())));
    }

    private void OnEquipedExamined(Entity<RMCClockComponent> ent, ref AccessoryRelayedEvent<ExaminedEvent> args)
    {
        args.Args.PushMarkup(Loc.GetString("rmc-clock-examine", ("device", ent.Owner), ("time", GetTime())));
    }
}
