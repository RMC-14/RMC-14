using System.Linq;
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
    }

    private void OnExamined(Entity<RMCClockComponent> ent, ref ExaminedEvent args)
    {
        var owner = ent.Owner;
        var date = DateTime.Now.AddYears(100).ToString("dd MMMM, yyyy");
        var worldTime = (EntityQuery<GlobalTimeManagerComponent>().FirstOrDefault()?.TimeOffset ?? TimeSpan.Zero) + _ticker.RoundDuration();
        var time = worldTime.ToString(@"hh\:mm");

        args.PushMarkup(Loc.GetString("rmc-clock-examine", ("device", owner), ("date", date), ("time", time)));
    }
}
