using System;
using System.Collections.Generic;
using System.Linq;
using Content.Server.Destructible;
using Content.Server.Destructible.Thresholds;
using Content.Server.Destructible.Thresholds.Triggers;
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
        var allowed = SharedSentryTargetingSystem.SentryAllowedFactions;
        return all.Where(x => allowed.Contains(x.Key)).ToDictionary(x => x.Key, x => x.Key);
    }

    private void OnViewCameraMsg(Entity<SentryLaptopComponent> laptop, ref SentryLaptopViewCameraBuiMsg args)
    {
        if (!TryGetEntity(args.Sentry, out var sentryUid))
            return;

        if (!GetLinkedSentries(laptop).Contains(sentryUid.Value))
            return;

        if (!TryComp<SentryComponent>(sentryUid.Value, out var sentry) || sentry.Mode == SentryMode.Item)
            return;

        if (!laptop.Comp.IsOpen || _containers.IsEntityInContainer(laptop.Owner))
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
            if (TryComp<SentryTargetingComponent>(sentryUid, out var targeting))
                _sentryTargetingServer.ToggleFaction((sentryUid, targeting), args.Faction, args.Targeted);
        }

        UpdateUI(laptop);
    }

    private void OnGlobalSetFactionsMsg(Entity<SentryLaptopComponent> laptop, ref SentryLaptopGlobalSetFactionsBuiMsg args)
    {
        var factionSet = args.Factions.ToHashSet();

        foreach (var sentryUid in GetLinkedSentries(laptop))
        {
            if (TryComp<SentryTargetingComponent>(sentryUid, out var targeting))
                _sentryTargetingServer.SetFriendlyFactions((sentryUid, targeting), factionSet);
        }

        UpdateUI(laptop);
    }

    private void OnGlobalResetTargetingMsg(Entity<SentryLaptopComponent> laptop, ref SentryLaptopGlobalResetTargetingBuiMsg args)
    {
        foreach (var sentryUid in GetLinkedSentries(laptop))
        {
            if (TryComp<SentryTargetingComponent>(sentryUid, out var targeting))
                _sentryTargetingServer.ResetToDefault((sentryUid, targeting));
        }

        UpdateUI(laptop);
    }

    private void OnGlobalTogglePowerMsg(Entity<SentryLaptopComponent> laptop, ref SentryLaptopGlobalTogglePowerBuiMsg args)
    {
        var newMode = args.PowerOn ? SentryMode.On : SentryMode.Off;

        foreach (var sentryUid in GetLinkedSentries(laptop))
        {
            if (!TryComp<SentryComponent>(sentryUid, out var sentry) || sentry.Mode == SentryMode.Item)
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

        RemoveViewSubscription(user, watcher);

        watcher.Laptop = null;
        watcher.CurrentSentry = null;
        Dirty(user, watcher);
        RemCompDeferred<SentryLaptopWatcherComponent>(user);
    }

    private void OnWatcherShutdown(Entity<SentryLaptopWatcherComponent> watcher, ref ComponentShutdown args)
    {
        if (!_actorQuery.TryComp(watcher.Owner, out var actor))
            return;

        RemoveViewSubscription(watcher.Owner, watcher.Comp, actor);

        watcher.Comp.Laptop = null;
        watcher.Comp.CurrentSentry = null;
    }

    private void OnWatcherDetached(Entity<SentryLaptopWatcherComponent> watcher, ref PlayerDetachedEvent args)
    {
        if (watcher.Comp.CurrentSentry is { } net && TryGetEntity(net, out var sentryUid))
            _viewSubscriber.RemoveViewSubscriber(sentryUid.Value, args.Player);

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
            if (!TryGetValidSentry(watcher, out var sentryUid))
            {
                ClearWatcher(watcherUid, watcher);
                continue;
            }

            if (!sentryUid.HasValue)
            {
                ClearWatcher(watcherUid, watcher);
                continue;
            }

            if (!TryComp<SentryComponent>(sentryUid.Value, out var sentry))
            {
                ClearWatcher(watcherUid, watcher);
                continue;
            }

            if (sentry.Mode == SentryMode.Item)
            {
                ClearWatcher(watcherUid, watcher);
            }
        }
    }

    private bool TryGetValidSentry(SentryLaptopWatcherComponent watcher, out EntityUid? sentryUid)
    {
        sentryUid = null;
        return watcher.CurrentSentry is { } net && TryGetEntity(net, out sentryUid);
    }

    private void ClearWatcher(EntityUid watcherUid, SentryLaptopWatcherComponent watcher)
    {
        RemoveViewSubscription(watcherUid, watcher);

        watcher.Laptop = null;
        watcher.CurrentSentry = null;
        Dirty(watcherUid, watcher);
        RemCompDeferred<SentryLaptopWatcherComponent>(watcherUid);
    }

    private void RemoveViewSubscription(EntityUid watcherUid, SentryLaptopWatcherComponent watcher, ActorComponent? actor = null)
    {
        if (watcher.CurrentSentry is not { } net || !TryGetEntity(net, out var sentryUid))
            return;

        if (actor == null && !_actorQuery.TryComp(watcherUid, out actor))
            return;

        _viewSubscriber.RemoveViewSubscriber(sentryUid.Value, actor.PlayerSession);
    }

    protected override float GetSentryVisionRadius(EntityUid sentry)
    {
        if (!TryComp<HTNComponent>(sentry, out var htn))
            return 5.0f;

        if (htn.Blackboard.TryGetValue<float>("VisionRadius", out var radius, EntityManager))
            return radius;

        return 5.0f;
    }

    protected override float GetSentryMaxHealth(EntityUid sentry)
    {
        if (!TryComp<DestructibleComponent>(sentry, out var destruct))
            return base.GetSentryMaxHealth(sentry);

        var max = 0f;
        foreach (var threshold in destruct.Thresholds)
        {
            if (threshold.Trigger is DamageTrigger damageTrigger)
                max = Math.Max(max, damageTrigger.Damage);
        }

        return max > 0f ? max : base.GetSentryMaxHealth(sentry);
    }
}
