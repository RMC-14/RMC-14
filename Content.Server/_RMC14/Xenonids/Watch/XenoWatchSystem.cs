using System.Linq;
using Content.Server.Chat.Systems;
using Content.Server.Popups;
using Content.Shared._RMC14.CCVar;
using Content.Shared._RMC14.Xenonids;
using Content.Shared._RMC14.Xenonids.Egg;
using Content.Shared._RMC14.Xenonids.Evolution;
using Content.Shared._RMC14.Xenonids.Heal;
using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared._RMC14.Xenonids.HiveLeader;
using Content.Shared._RMC14.Xenonids.Plasma;
using Content.Shared._RMC14.Xenonids.Watch;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Robust.Server.GameObjects;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using static Content.Server.Chat.Systems.ChatSystem;

namespace Content.Server._RMC14.Xenonids.Watch;

public sealed class XenoWatchSystem : SharedXenoWatchSystem
{
    [Dependency] private readonly SharedEyeSystem _eye = default!;
    [Dependency] private readonly SharedXenoHiveSystem _hive = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly ViewSubscriberSystem _viewSubscriber = default!;
    [Dependency] private readonly XenoEvolutionSystem _xenoEvolution = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly MobThresholdSystem _threshold = default!;
    [Dependency] private readonly XenoPlasmaSystem _plasma = default!;
    [Dependency] private readonly MobThresholdSystem _threshhold = default!;

    private EntityQuery<ActorComponent> _actorQuery;
    private EntityQuery<XenoWatchedComponent> _xenoWatchedQuery;

    private TimeSpan _maxProcessTime;
    private TimeSpan _nextUpdateTime;
    private TimeSpan _updateEvery;


    private readonly Dictionary<ICommonSession, ICChatRecipientData> _recipients = new();

    public override void Initialize()
    {
        base.Initialize();

        _actorQuery = GetEntityQuery<ActorComponent>();
        _xenoWatchedQuery = GetEntityQuery<XenoWatchedComponent>();

        SubscribeLocalEvent<XenoWatchedComponent, ComponentRemove>(OnWatchedRemove);
        SubscribeLocalEvent<XenoWatchedComponent, EntityTerminatingEvent>(OnWatchedRemove);

        SubscribeLocalEvent<XenoWatchingComponent, ComponentRemove>(OnWatchingRemove);
        SubscribeLocalEvent<XenoWatchingComponent, EntityTerminatingEvent>(OnWatchingRemove);
        SubscribeLocalEvent<XenoComponent, WatchInfoUpdateEvent>(UpdateInfo);


        SubscribeLocalEvent<ExpandICChatRecipientsEvent>(OnExpandRecipients);

        Subs.CVar(_config, RMCCVars.RMCXenoWatchUpdateEverySeconds, v => _updateEvery = TimeSpan.FromSeconds(v), true);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var time = _timing.CurTime;
        if (time < _nextUpdateTime)
            return;

        _nextUpdateTime = time + _updateEvery;
        var query = EntityQueryEnumerator<XenoComponent, HiveMemberComponent, MetaDataComponent>();
        while (query.MoveNext(out var uid, out _, out var member, out var metaData))
        {
            if (TryComp<XenoComponent>(uid, out var comp))
            {
                if (comp.Refresh)
                {
                    var updateEvent = new WatchInfoUpdateEvent();
                    RaiseLocalEvent(uid, ref updateEvent);
                }
            }
        }
    }

    private void OnWatchedRemove<T>(Entity<XenoWatchedComponent> ent, ref T args)
    {
        foreach (var watching in ent.Comp.Watching)
        {
            if (TerminatingOrDeleted(watching))
                continue;

            RemCompDeferred<XenoWatchingComponent>(watching);
        }
    }

    private void OnWatchingRemove<T>(Entity<XenoWatchingComponent> ent, ref T args)
    {
        RemoveWatcher(ent);
    }

    private void OnExpandRecipients(ExpandICChatRecipientsEvent ev)
    {
        var sourceTransform = Transform(ev.Source);
        var sourcePos = _transform.GetWorldPosition(sourceTransform);

        _recipients.Clear();
        foreach (var session in ev.Recipients)
        {
            if (session.Key.AttachedEntity is not { } recipient ||
                !_xenoWatchedQuery.TryComp(recipient, out var watched))
            {
                continue;
            }

            var recipientTransform = Transform(recipient);
            if (sourceTransform.MapID != recipientTransform.MapID)
                continue;

            var recipientPos = _transform.GetWorldPosition(recipientTransform);
            var range = (sourcePos - recipientPos).Length();
            foreach (var watching in watched.Watching)
            {
                if (!_actorQuery.TryComp(watching, out var actor))
                    continue;

                if (!ev.Recipients.ContainsKey(actor.PlayerSession))
                    _recipients.TryAdd(actor.PlayerSession, new ICChatRecipientData(range, false, true));
            }
        }

        foreach (var recipient in _recipients)
        {
            ev.Recipients.TryAdd(recipient.Key, recipient.Value);
        }
    }

    protected override void OnXenoWatchAction(Entity<XenoComponent> ent, ref XenoWatchActionEvent args)
    {
        args.Handled = true;

        if (_hive.GetHive(ent.Owner) is not {} hive)
            return;

        if (!HasQueenPopup(ent))
            return;

        _ui.OpenUi(ent.Owner, XenoWatchUIKey.Key, ent);

        var xenos = new List<Xeno>();

        var query = EntityQueryEnumerator<XenoComponent, HiveMemberComponent, MetaDataComponent>();
        while (query.MoveNext(out var uid, out _, out var member, out var metaData))
        {
            if (uid == ent.Owner || member.Hive != hive.Owner)
                continue;

            if (_mobState.IsDead(uid))
                continue;

            xenos.Add(new Xeno(GetNetEntity(uid), Name(uid, metaData), metaData.EntityPrototype?.ID, 0, 0, 0));
        }

        xenos.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));

        _ui.SetUiState(ent.Owner, XenoWatchUIKey.Key, new XenoWatchBuiState(xenos, hive.Comp.BurrowedLarva,hive.Comp.BurrowedLarvaSlotFactor,0,0,0));
    }

    private void UpdateInfo(Entity<XenoComponent> ent, ref WatchInfoUpdateEvent args)
    {
        if (_hive.GetHive(ent.Owner) is not {} hive)
            return;

        short tier3Amount = 0;
        short tier2Amount = 0;
        short xenocount = 0;
        bool queen = TryComp<XenoOvipositorCapableComponent>(ent, out var ovicomp) && ovicomp.Attached;


        var xenos = new List<Xeno>();
        var query = EntityQueryEnumerator<XenoComponent, HiveMemberComponent, MetaDataComponent>();
        while (query.MoveNext(out var uid, out var comp, out var member, out var metaData))
        {
            bool leader = false;
            if (uid == ent.Owner || member.Hive != hive.Owner)
                continue;

            if (_mobState.IsDead(uid))
                continue;

            if(TryComp<HiveLeaderComponent>(uid, out var leaderComp))
                leader = true;

            if (comp.CountedInSlots)
            {
                xenocount++;
            }

            switch (comp.Tier)
            {
                case 2:
                    tier2Amount++;
                    break;
                case 3:
                    tier3Amount++;
                    break;
            }


            FixedPoint2 evo = 0;

            if (TryComp<XenoEvolutionComponent>(uid, out var evoComp))
            {
                evo = evoComp.Points;
            }
            if(!TryComp<DamageableComponent>(uid, out var damageableComp))
                return;

            _threshhold.TryGetIncapPercentage(uid, damageableComp.TotalDamage, out var incap);

            xenos.Add(new Xeno(GetNetEntity(uid), Name(uid, metaData), metaData.EntityPrototype?.ID,(1-incap)??0 , _plasma.GetPlasmaPercentage(uid), evo, leader));
        }


        _ui.SetUiState(ent.Owner, XenoWatchUIKey.Key, new XenoWatchBuiState(xenos,  hive.Comp.BurrowedLarva, hive.Comp.BurrowedLarvaSlotFactor, xenocount, tier2Amount, tier3Amount, queen));
    }


    public override void Watch(Entity<HiveMemberComponent?, ActorComponent?, EyeComponent?> watcher, Entity<HiveMemberComponent?> toWatch)
    {
        base.Watch(watcher, toWatch);

        if (!HasQueenPopup(watcher))
            return;

        if (watcher.Owner == toWatch.Owner)
            return;

        if (!_hive.FromSameHive((watcher, watcher.Comp1), toWatch))
            return;

        if (!Resolve(watcher, ref watcher.Comp2, false))
            return;

        _eye.SetTarget(watcher, toWatch, watcher);
        _viewSubscriber.AddViewSubscriber(toWatch, watcher.Comp2.PlayerSession);

        RemoveWatcher(watcher);
        EnsureComp<XenoWatchingComponent>(watcher).Watching = toWatch;
        EnsureComp<XenoWatchedComponent>(toWatch).Watching.Add(watcher);

        var ev = new XenoWatchEvent();
        RaiseLocalEvent(watcher, ref ev);
    }

    protected override void Unwatch(Entity<EyeComponent?> watcher, ICommonSession player)
    {
        if (!Resolve(watcher, ref watcher.Comp))
            return;

        var oldTarget = watcher.Comp.Target;

        base.Unwatch(watcher, player);

        if (oldTarget != null && oldTarget != watcher.Owner)
            _viewSubscriber.RemoveViewSubscriber(oldTarget.Value, player);

        RemoveWatcher(watcher);
    }

    private void RemoveWatcher(EntityUid toRemove)
    {
        if (!TryComp(toRemove, out XenoWatchingComponent? watching))
            return;

        if (TryComp(watching.Watching, out XenoWatchedComponent? watched))
        {
            watched.Watching.Remove(toRemove);
            if (watched.Watching.Count == 0)
                RemCompDeferred<XenoWatchedComponent>(watching.Watching.Value);
        }

        watching.Watching = null;
        RemCompDeferred<XenoWatchingComponent>(toRemove);
    }

    private bool HasQueenPopup(EntityUid xeno)
    {
        if (_xenoEvolution.HasLiving<XenoEvolutionGranterComponent>(1))
            return true;

        _popup.PopupEntity(Loc.GetString("rmc-no-queen-hivemind-chat"), xeno, xeno, PopupType.MediumCaution);
        return false;
    }
}
