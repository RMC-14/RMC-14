using System;
using System.Collections.Generic;
using System.Linq;
using Content.Server.NPC.HTN;
using Content.Shared._RMC14.Areas;
using Content.Shared._RMC14.Sentry;
using Content.Shared._RMC14.Sentry.Laptop;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.NPC.Prototypes;
using Content.Shared.NPC.Systems;
using Content.Shared.UserInterface;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server._RMC14.Sentry.Laptop;

public sealed class SentryLaptopSystem : SharedSentryLaptopSystem
{
    [Dependency] private readonly AreaSystem _areaServer = default!;
    [Dependency] private readonly SharedSentryTargetingSystem _sentryTargetingServer = default!;
    [Dependency] private readonly UserInterfaceSystem _uiServer = default!;
    [Dependency] private readonly SharedTransformSystem _transformServer = default!;
    [Dependency] private readonly IGameTiming _timingServer = default!;
    [Dependency] private readonly ViewSubscriberSystem _viewSubscriber = default!;
    [Dependency] private readonly NpcFactionSystem _faction = default!;
    [Dependency] private readonly SharedContainerSystem _containers = default!;

    private EntityQuery<ActorComponent> _actorQuery;

    private float _updateTimer;
    private const float UpdateInterval = 1.0f;

    public override void Initialize()
    {
        base.Initialize();

        _actorQuery = GetEntityQuery<ActorComponent>();

        SubscribeLocalEvent<SentryLaptopComponent, AfterActivatableUIOpenEvent>(OnUIOpened);
        SubscribeLocalEvent<SentryLaptopComponent, BoundUIOpenedEvent>(OnBoundUIOpened);
        SubscribeLocalEvent<SentryLaptopComponent, SentryLaptopViewCameraBuiMsg>(OnViewCameraMsg);
        SubscribeLocalEvent<SentryLaptopComponent, SentryLaptopSetNameBuiMsg>(OnSetNameMsg);
        SubscribeLocalEvent<SentryLaptopComponent, SentryLaptopGlobalToggleFactionBuiMsg>(OnGlobalToggleFactionMsg);
        SubscribeLocalEvent<SentryLaptopComponent, SentryLaptopGlobalSetFactionsBuiMsg>(OnGlobalSetFactionsMsg);
        SubscribeLocalEvent<SentryLaptopComponent, SentryLaptopGlobalResetTargetingBuiMsg>(OnGlobalResetTargetingMsg);
        SubscribeLocalEvent<SentryLaptopComponent, SentryLaptopGlobalTogglePowerBuiMsg>(OnGlobalTogglePowerMsg);
        SubscribeLocalEvent<SentryLaptopComponent, SentryLaptopCloseCameraBuiMsg>(OnCloseCameraMsg);
        SubscribeLocalEvent<SentryComponent, ComponentShutdown>(OnSentryShutdown);

        SubscribeLocalEvent<SentryLaptopWatcherComponent, ComponentShutdown>(OnWatcherShutdown);
        SubscribeLocalEvent<SentryLaptopWatcherComponent, PlayerDetachedEvent>(OnWatcherDetached);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        _updateTimer += frameTime;
        if (_updateTimer < UpdateInterval)
            return;

        _updateTimer = 0f;

        UpdateAllOpenUIsServer();
        CleanupInvalidCameraWatchers();
    }

    private void UpdateAllOpenUIsServer()
    {
        var query = EntityQueryEnumerator<SentryLaptopComponent>();
        while (query.MoveNext(out var uid, out var laptop))
        {
            if (_uiServer.IsUiOpen(uid, SentryLaptopUiKey.Key))
                UpdateUI((uid, laptop));
        }
    }

    private void OnUIOpened(Entity<SentryLaptopComponent> laptop, ref AfterActivatableUIOpenEvent args)
    {
        UpdateUI(laptop);
    }

    private void OnBoundUIOpened(Entity<SentryLaptopComponent> laptop, ref BoundUIOpenedEvent args)
    {
        UpdateUI(laptop);
    }

    protected override void UpdateUI(Entity<SentryLaptopComponent> laptop)
    {
        if (!_uiServer.IsUiOpen(laptop.Owner, SentryLaptopUiKey.Key))
            return;

        var sentries = BuildSentryInfoList(laptop);
        var factions = GetFactionList();
        factions["Humanoid"] = "Humanoid";

        var state = new SentryLaptopBuiState(
            sentries,
            factions.Keys.ToList(),
            factions
        );

        _uiServer.SetUiState(laptop.Owner, SentryLaptopUiKey.Key, state);
    }

    private Dictionary<string, string> GetFactionList()
    {
        var all = _faction.GetFactions();
        var dummy = all.GetValueOrDefault("RMCDumb", new FactionData());
        var hostile = all.Where(x => dummy.Hostile.Contains(x.Key)).ToDictionary(x => x.Key, x => x.Key);
        return hostile;
    }

    private void OnViewCameraMsg(Entity<SentryLaptopComponent> laptop, ref SentryLaptopViewCameraBuiMsg args)
    {
        if (!TryGetEntity(args.Sentry, out var sentryUid))
            return;

        if (!GetLinkedSentries(laptop).Contains(sentryUid.Value))
            return;

        if (!TryComp<SentryComponent>(sentryUid.Value, out var sentry) || sentry.Mode == SentryMode.Item)
            return;

        if (!laptop.Comp.IsOpen)
            return;

        if (_containers.IsEntityInContainer(laptop.Owner))
            return;

        var user = args.Actor;
        if (!_actorQuery.TryComp(user, out var actor))
            return;

        if (!TryComp<SentryLaptopWatcherComponent>(user, out var watcher))
            watcher = EnsureComp<SentryLaptopWatcherComponent>(user);

        if (watcher.CurrentSentry is { } oldNet && TryGetEntity(oldNet, out var oldSentry))
            _viewSubscriber.RemoveViewSubscriber(oldSentry.Value, actor.PlayerSession);

        watcher.Laptop = laptop.Owner;
        watcher.CurrentSentry = GetNetEntity(sentryUid.Value);
        Dirty(user, watcher);

        _viewSubscriber.AddViewSubscriber(sentryUid.Value, actor.PlayerSession);
    }

    private void OnSetNameMsg(Entity<SentryLaptopComponent> laptop, ref SentryLaptopSetNameBuiMsg args)
    {
        if (!TryGetEntity(args.Sentry, out var sentryUid))
            return;

        if (!GetLinkedSentries(laptop).Contains(sentryUid.Value))
            return;

        var name = args.Name.Trim();
        if (name.Length > 50)
            name = name[..50];

        if (string.IsNullOrWhiteSpace(name))
            laptop.Comp.SentryCustomNames.Remove(sentryUid.Value);
        else
            laptop.Comp.SentryCustomNames[sentryUid.Value] = name;

        Dirty(laptop);
        UpdateUI(laptop);
    }

    private void OnGlobalToggleFactionMsg(Entity<SentryLaptopComponent> laptop, ref SentryLaptopGlobalToggleFactionBuiMsg args)
    {
        foreach (var sentryUid in GetLinkedSentries(laptop))
        {
            if (!TryComp<SentryTargetingComponent>(sentryUid, out var targeting))
                continue;

            _sentryTargetingServer.ToggleFaction((sentryUid, targeting), args.Faction, args.Targeted);
        }

        UpdateUI(laptop);
    }

    private void OnGlobalSetFactionsMsg(Entity<SentryLaptopComponent> laptop, ref SentryLaptopGlobalSetFactionsBuiMsg args)
    {
        var factionSet = args.Factions.ToHashSet();

        foreach (var sentryUid in GetLinkedSentries(laptop))
        {
            if (!TryComp<SentryTargetingComponent>(sentryUid, out var targeting))
                continue;

            _sentryTargetingServer.SetFriendlyFactions((sentryUid, targeting), factionSet);
        }

        UpdateUI(laptop);
    }

    private void OnGlobalResetTargetingMsg(Entity<SentryLaptopComponent> laptop, ref SentryLaptopGlobalResetTargetingBuiMsg args)
    {
        foreach (var sentryUid in GetLinkedSentries(laptop))
        {
            if (!TryComp<SentryTargetingComponent>(sentryUid, out var targeting))
                continue;

            _sentryTargetingServer.ResetToDefault((sentryUid, targeting));
        }

        UpdateUI(laptop);
    }

    private void OnGlobalTogglePowerMsg(Entity<SentryLaptopComponent> laptop, ref SentryLaptopGlobalTogglePowerBuiMsg args)
    {
        var newMode = args.PowerOn ? SentryMode.On : SentryMode.Off;

        foreach (var sentryUid in GetLinkedSentries(laptop))
        {
            if (!TryComp<SentryComponent>(sentryUid, out var sentry))
                continue;

            if (sentry.Mode == SentryMode.Item)
                continue;

            EntityManager.System<SentrySystem>().TrySetMode((sentryUid, sentry), newMode);
        }

        UpdateUI(laptop);
    }

    private void OnCloseCameraMsg(Entity<SentryLaptopComponent> laptop, ref SentryLaptopCloseCameraBuiMsg args)
    {
        var user = args.Actor;

        if (!TryComp<SentryLaptopWatcherComponent>(user, out var watcher))
            return;

        if (_actorQuery.TryComp(user, out var actor) &&
            watcher.CurrentSentry is { } net &&
            TryGetEntity(net, out var sentryUid))
        {
            _viewSubscriber.RemoveViewSubscriber(sentryUid.Value, actor.PlayerSession);
        }

        watcher.Laptop = null;
        watcher.CurrentSentry = null;
        Dirty(user, watcher);
        RemCompDeferred<SentryLaptopWatcherComponent>(user);
    }

    private void OnWatcherShutdown(Entity<SentryLaptopWatcherComponent> watcher, ref ComponentShutdown args)
    {
        if (!_actorQuery.TryComp(watcher.Owner, out var actor))
            return;

        if (watcher.Comp.CurrentSentry is { } net && TryGetEntity(net, out var sentryUid))
            _viewSubscriber.RemoveViewSubscriber((EntityUid)sentryUid, actor.PlayerSession);

        watcher.Comp.Laptop = null;
        watcher.Comp.CurrentSentry = null;
    }

    private void OnWatcherDetached(Entity<SentryLaptopWatcherComponent> watcher, ref PlayerDetachedEvent args)
    {
        if (watcher.Comp.CurrentSentry is { } net && TryGetEntity(net, out var sentryUid))
            _viewSubscriber.RemoveViewSubscriber((EntityUid)sentryUid, args.Player);

        watcher.Comp.Laptop = null;
        watcher.Comp.CurrentSentry = null;
    }

    private void OnSentryShutdown(Entity<SentryComponent> sentry, ref ComponentShutdown args)
    {
        var sentryNet = GetNetEntity(sentry.Owner);
        var watchers = EntityQueryEnumerator<SentryLaptopWatcherComponent>();
        while (watchers.MoveNext(out var watcherUid, out var watcher))
        {
            if (watcher.CurrentSentry != sentryNet)
                continue;

            if (_actorQuery.TryComp(watcherUid, out var actor))
                _viewSubscriber.RemoveViewSubscriber(sentry.Owner, actor.PlayerSession);

            watcher.Laptop = null;
            watcher.CurrentSentry = null;
            Dirty(watcherUid, watcher);
            RemCompDeferred<SentryLaptopWatcherComponent>(watcherUid);
        }
    }

    private void CleanupInvalidCameraWatchers()
    {
        var watchers = EntityQueryEnumerator<SentryLaptopWatcherComponent>();
        while (watchers.MoveNext(out var watcherUid, out var watcher))
        {
            if (watcher.CurrentSentry is not { } net || !TryGetEntity(net, out var sentryUid))
            {
                ClearWatcher(watcherUid, watcher);
                continue;
            }

            if (!TryComp<SentryComponent>(sentryUid.Value, out var sentry) || sentry.Mode == SentryMode.Item)
            {
                ClearWatcher(watcherUid, watcher);
            }
        }
    }

    private void ClearWatcher(EntityUid watcherUid, SentryLaptopWatcherComponent watcher)
    {
        if (_actorQuery.TryComp(watcherUid, out var actor) &&
            watcher.CurrentSentry is { } net &&
            TryGetEntity(net, out var sentryUid))
        {
            _viewSubscriber.RemoveViewSubscriber(sentryUid.Value, actor.PlayerSession);
        }

        watcher.Laptop = null;
        watcher.CurrentSentry = null;
        Dirty(watcherUid, watcher);
        RemCompDeferred<SentryLaptopWatcherComponent>(watcherUid);
    }

    private float GetSentryVisionRadius(EntityUid sentry)
    {
        if (!TryComp<HTNComponent>(sentry, out var htn))
            return 5.0f;

        if (htn.Blackboard.TryGetValue<float>("VisionRadius", out var radius, EntityManager))
            return radius;

        return 5.0f;
    }
}
