using Content.Server.Chat.Systems;
using Content.Server.Popups;
using Content.Shared._RMC14.Xenonids;
using Content.Shared._RMC14.Xenonids.Evolution;
using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared._RMC14.Xenonids.Watch;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Robust.Server.GameObjects;
using Robust.Shared.Player;
using static Content.Server.Chat.Systems.ChatSystem;

namespace Content.Server._RMC14.Xenonids.Watch;

public sealed class XenoWatchSystem : SharedWatchXenoSystem
{
    [Dependency] private readonly SharedEyeSystem _eye = default!;
    [Dependency] private readonly SharedXenoHiveSystem _hive = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly ViewSubscriberSystem _viewSubscriber = default!;
    [Dependency] private readonly XenoEvolutionSystem _xenoEvolution = default!;

    private EntityQuery<ActorComponent> _actorQuery;
    private EntityQuery<XenoWatchedComponent> _xenoWatchedQuery;

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

        SubscribeLocalEvent<ExpandICChatRecipientsEvent>(OnExpandRecipients);
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

            xenos.Add(new Xeno(GetNetEntity(uid), Name(uid, metaData), metaData.EntityPrototype?.ID));
        }

        xenos.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));

        _ui.SetUiState(ent.Owner, XenoWatchUIKey.Key, new XenoWatchBuiState(xenos, hive.Comp.BurrowedLarva));
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
