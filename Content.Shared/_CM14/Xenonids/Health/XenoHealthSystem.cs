using Content.Shared.Examine;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared._CM14.Xenonids.Health;

public sealed class XenoHealthSystem : EntitySystem
{
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly MobThresholdSystem _mobThreshold = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<XenoTimeHealthComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<XenoTimeHealthComponent, ExaminedEvent>(OnExamined);
    }

    private void OnMapInit(Entity<XenoTimeHealthComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.ChangeAt = _timing.CurTime + ent.Comp.Delay;
        Dirty(ent);

        var meta = MetaData(ent);
        if (meta.EntityName.StartsWith(ent.Comp.NamePrefix))
            return;

        _metaData.SetEntityName(ent, $"{ent.Comp.NamePrefix} {meta.EntityName}", meta);
    }

    private void OnExamined(Entity<XenoTimeHealthComponent> ent, ref ExaminedEvent args)
    {
        if (!HasComp<XenoComponent>(args.Examiner))
            return;

        var time = ent.Comp.ChangeAt - _timing.CurTime;
        if (time <= TimeSpan.Zero)
            return;

        using (args.PushGroup(nameof(XenoHealthSystem)))
        {
            var minutes = (int) time.TotalMinutes;
            var seconds = time.Seconds;
            var timeString = string.Empty;
            if (minutes > 0)
                timeString += $" {minutes} minutes";

            if (seconds > 0)
                timeString += $" {seconds} seconds";

            args.PushText($"Matures in{timeString}");
        }
    }

    public override void Update(float frameTime)
    {
        if (_net.IsClient)
            return;

        var time = _timing.CurTime;
        var query = EntityQueryEnumerator<XenoTimeHealthComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (time < comp.ChangeAt)
                return;

            _mobThreshold.SetMobStateThreshold(uid, comp.DeadThreshold, MobState.Dead);
            _mobThreshold.SetMobStateThreshold(uid, comp.CritThreshold, MobState.Critical);

            var meta = MetaData(uid);
            if (meta.EntityName.StartsWith(comp.NamePrefix))
                _metaData.SetEntityName(uid, meta.EntityName[comp.NamePrefix.Length..].Trim(), meta);

            RemCompDeferred<XenoTimeHealthComponent>(uid);
        }
    }
}
