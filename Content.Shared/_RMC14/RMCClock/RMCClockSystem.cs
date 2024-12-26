using System.Linq;
using Content.Shared.Clock;
using Content.Shared.Examine;
using Content.Shared.GameTicking;

namespace Content.Shared._RMC14.RMCClock;

public abstract class RMCClockSystem : EntitySystem
{
    [Dependency] private readonly SharedGameTicker _ticker = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCClockComponent, ExaminedEvent>(OnExamined);
    }

    private void OnExamined(Entity<RMCClockComponent> ent, ref ExaminedEvent args)
    {
            args.PushText(GetClockTimeText(ent));
    }

    public string GetClockTimeText(Entity<RMCClockComponent> ent)
    {
        var time = (EntityQuery<GlobalTimeManagerComponent>().FirstOrDefault()?.TimeOffset ?? TimeSpan.Zero) + _ticker.RoundDuration();

        var date = DateTime.Now.AddYears(100).ToString("dd MMMM, yyyy");

        return $"The {ent} reads {date} - {time:hh\\:mm}";
    }
}
