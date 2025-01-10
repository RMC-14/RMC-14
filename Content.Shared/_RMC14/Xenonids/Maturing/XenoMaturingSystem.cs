using Content.Shared.Actions;
using Content.Shared.Examine;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.NameModifier.EntitySystems;
using Content.Shared.Popups;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Xenonids.Maturing;

public sealed class XenoMaturingSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly MobThresholdSystem _mobThreshold = default!;
    [Dependency] private readonly NameModifierSystem _nameModifier = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<XenoMaturingComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<XenoMaturingComponent, ComponentRemove>(OnRemove);
        SubscribeLocalEvent<XenoMaturingComponent, RefreshNameModifiersEvent>(OnRefreshNameModifiers);
        SubscribeLocalEvent<XenoMaturingComponent, ExaminedEvent>(OnExamined);
    }

    private void OnMapInit(Entity<XenoMaturingComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.MatureAt = _timing.CurTime + ent.Comp.Delay;
        Dirty(ent);
        _nameModifier.RefreshNameModifiers(ent.Owner);
    }

    private void OnRemove(Entity<XenoMaturingComponent> ent, ref ComponentRemove args)
    {
        if (TerminatingOrDeleted(ent))
            return;

        _nameModifier.RefreshNameModifiers(ent.Owner);
    }

    private void OnRefreshNameModifiers(Entity<XenoMaturingComponent> ent, ref RefreshNameModifiersEvent args)
    {
        args.AddModifier("rmc-xeno-immature-prefix");
    }

    private void OnExamined(Entity<XenoMaturingComponent> ent, ref ExaminedEvent args)
    {
        if (!HasComp<XenoComponent>(args.Examiner))
            return;

        var time = ent.Comp.MatureAt - _timing.CurTime;
        if (time <= TimeSpan.Zero)
            return;

        using (args.PushGroup(nameof(XenoMaturingSystem)))
        {
            var minutes = (int) time.TotalMinutes;
            var seconds = time.Seconds;
            if (minutes > 0)
                args.PushText(Loc.GetString("rmc-xeno-immature-matures-in-minutes", ("minutes", minutes), ("seconds", seconds)));
            else if (seconds > 0)
                args.PushText(Loc.GetString("rmc-xeno-immature-matures-in-seconds", ("seconds", seconds)));
        }
    }

    public void Mature(Entity<XenoMaturingComponent> maturing)
    {
        maturing.Comp.MatureAt = TimeSpan.Zero;
        Dirty(maturing);
    }

    public override void Update(float frameTime)
    {
        if (_net.IsClient)
            return;

        var time = _timing.CurTime;
        var query = EntityQueryEnumerator<XenoMaturingComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (time < comp.MatureAt)
                return;

            _mobThreshold.SetMobStateThreshold(uid, comp.DeadThreshold, MobState.Dead);
            _mobThreshold.SetMobStateThreshold(uid, comp.CritThreshold, MobState.Critical);

            EntityManager.AddComponents(uid, comp.AddComponents);
            foreach (var action in comp.AddActions)
            {
                _actions.AddAction(uid, action);
            }

            _popup.PopupEntity(Loc.GetString("rmc-xeno-immature-mature"), uid, uid, PopupType.Large);
            RemCompDeferred<XenoMaturingComponent>(uid);
        }
    }
}
