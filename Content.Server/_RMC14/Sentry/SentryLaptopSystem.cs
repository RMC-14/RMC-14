using System;
using System.Collections.Generic;
using System.Linq;
using Content.Server.Destructible;
using Content.Server.NPC.HTN;
using Content.Shared._RMC14.Areas;
using Content.Shared._RMC14.Sentry;
using Content.Shared._RMC14.Sentry.Laptop;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Damage;
using Content.Shared.UserInterface;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using SentryAlertEvent = Content.Shared._RMC14.Sentry.Laptop.SentryAlertEvent;
using SentryAlertType = Content.Shared._RMC14.Sentry.Laptop.SentryAlertType;

namespace Content.Server._RMC14.Sentry.Laptop;

public sealed class SentryLaptopSystem : SharedSentryLaptopSystem
{
    [Dependency] private readonly AreaSystem _area = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly DestructibleSystem _destructible = default!;
    [Dependency] private readonly SharedSentryTargetingSystem _sentryTargeting = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly ViewSubscriberSystem _viewSubscriber = default!;

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
        SubscribeLocalEvent<SentryLaptopComponent, SentryLaptopGlobalResetTargetingBuiMsg>(OnGlobalResetTargetingMsg);
        SubscribeLocalEvent<SentryLaptopComponent, SentryLaptopGlobalTogglePowerBuiMsg>(OnGlobalTogglePowerMsg);
        SubscribeLocalEvent<SentryLaptopComponent, SentryLaptopCloseCameraBuiMsg>(OnCloseCameraMsg);

        SubscribeLocalEvent<SentryComponent, DamageChangedEvent>(OnSentryDamageChanged);
        SubscribeLocalEvent<SentryComponent, GunShotEvent>(OnSentryShot);

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

        UpdateAllOpenUIs();
        CheckSentryAlerts();
    }

    private void UpdateAllOpenUIs()
    {
        var query = EntityQueryEnumerator<SentryLaptopComponent>();
        while (query.MoveNext(out var uid, out var laptop))
        {
            if (_ui.IsUiOpen(uid, SentryLaptopUiKey.Key))
                UpdateUI((uid, laptop));
        }
    }

    private void CheckSentryAlerts()
    {
        var time = _timing.CurTime;
        var laptopQuery = EntityQueryEnumerator<SentryLaptopComponent>();

        while (laptopQuery.MoveNext(out var laptopUid, out var laptop))
        {
            if (!laptop.IsPowered || !_ui.IsUiOpen(laptopUid, SentryLaptopUiKey.Key))
                continue;

            foreach (var sentryUid in laptop.LinkedSentries)
            {
                if (!TryComp<SentryComponent>(sentryUid, out var sentry))
                    continue;

                var ammo = GetSentryAmmo(sentryUid, out var maxAmmo);
                if (maxAmmo > 0 && (float)ammo / maxAmmo <= sentry.LowAmmoThreshold)
                {
                    if (time - sentry.LastLowAmmoAlert > sentry.AlertCooldown)
                    {
                        sentry.LastLowAmmoAlert = time;

                        SendAlert(laptopUid, sentryUid, SentryAlertType.LowAmmo,
                            $"{GetSentryDisplayName((laptopUid, laptop), sentryUid)}: LOW AMMO ({ammo}/{maxAmmo})");
                    }
                }

                var health = GetSentryHealth(sentryUid, out var maxHealth);
                if (maxHealth > 0 && health / maxHealth <= sentry.CriticalHealthThreshold)
                {
                    if (time - sentry.LastHealthAlert > sentry.AlertCooldown)
                    {
                        sentry.LastHealthAlert = time;

                        SendAlert(laptopUid, sentryUid, SentryAlertType.CriticalHealth,
                            $"{GetSentryDisplayName((laptopUid, laptop), sentryUid)}: CRITICAL DAMAGE");
                    }
                }
            }
        }
    }

    private void OnSentryDamageChanged(Entity<SentryComponent> sentry, ref DamageChangedEvent args)
    {
        if (!args.DamageIncreased || args.DamageDelta == null)
            return;

        var time = _timing.CurTime;
        if (time - sentry.Comp.LastHealthAlert < sentry.Comp.AlertCooldown)
            return;

        sentry.Comp.LastHealthAlert = time;

        if (!TryComp<SentryLaptopLinkedComponent>(sentry, out var linked) || linked.LinkedLaptop == null)
            return;

        if (!TryComp<SentryLaptopComponent>(linked.LinkedLaptop.Value, out var laptop))
            return;

        var health = GetSentryHealth(sentry, out var maxHealth);
        var healthPercent = maxHealth > 0 ? (int)((health / maxHealth) * 100) : 0;

        SendAlert(linked.LinkedLaptop.Value, sentry, SentryAlertType.Damaged,
            $"{GetSentryDisplayName((linked.LinkedLaptop.Value, laptop), sentry)}: Taking damage! ({healthPercent}% health)");
    }

    private void OnSentryShot(Entity<SentryComponent> sentry, ref GunShotEvent args)
    {
        var time = _timing.CurTime;
        if (time - sentry.Comp.LastTargetAlert < sentry.Comp.AlertCooldown)
            return;

        if (!TryComp<GunComponent>(sentry, out var gun) || gun.Target == null)
            return;

        sentry.Comp.LastTargetAlert = time;

        if (!TryComp<SentryLaptopLinkedComponent>(sentry, out var linked) || linked.LinkedLaptop == null)
            return;

        if (!TryComp<SentryLaptopComponent>(linked.LinkedLaptop.Value, out var laptop))
            return;

        var targetName = Name(gun.Target.Value);
        SendAlert(linked.LinkedLaptop.Value, sentry, SentryAlertType.TargetAcquired,
            $"{GetSentryDisplayName((linked.LinkedLaptop.Value, laptop), sentry)}: Engaging {targetName}");
    }

    private void SendAlert(EntityUid laptop, EntityUid sentry, SentryAlertType alertType, string message)
    {
        if (!_ui.IsUiOpen(laptop, SentryLaptopUiKey.Key))
            return;

        var alert = new SentryAlertEvent(GetNetEntity(sentry), alertType, message);
        _ui.ServerSendUiMessage(laptop, SentryLaptopUiKey.Key, alert);
    }

    private void OnUIOpened(Entity<SentryLaptopComponent> laptop, ref AfterActivatableUIOpenEvent args)
    {
        UpdateUI(laptop);
    }

    private void OnBoundUIOpened(Entity<SentryLaptopComponent> laptop, ref BoundUIOpenedEvent args)
    {
        UpdateUI(laptop);
    }

    private void OnViewCameraMsg(Entity<SentryLaptopComponent> laptop, ref SentryLaptopViewCameraBuiMsg args)
    {
        if (!TryGetEntity(args.Sentry, out var sentryUid))
            return;

        if (!laptop.Comp.LinkedSentries.Contains(sentryUid.Value))
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

    private void OnCloseCameraMsg(Entity<SentryLaptopComponent> laptop, ref SentryLaptopCloseCameraBuiMsg args)
    {
        var user = args.Actor;

        if (!TryComp<SentryLaptopWatcherComponent>(user, out var watcher))
            return;

        if (_actorQuery.TryComp(user, out var actor) && watcher.CurrentSentry is { } net && TryGetEntity(net, out var sentryUid))
            _viewSubscriber.RemoveViewSubscriber(sentryUid.Value, actor.PlayerSession);

        watcher.Laptop = null;
        watcher.CurrentSentry = null;
        Dirty(user, watcher);
        RemCompDeferred<SentryLaptopWatcherComponent>(user);
    }

    private void OnSetNameMsg(Entity<SentryLaptopComponent> laptop, ref SentryLaptopSetNameBuiMsg args)
    {
        if (!TryGetEntity(args.Sentry, out var sentryUid))
            return;

        if (!laptop.Comp.LinkedSentries.Contains(sentryUid.Value))
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
        foreach (var sentryUid in laptop.Comp.LinkedSentries)
        {
            if (!TryComp<SentryTargetingComponent>(sentryUid, out var targeting))
                continue;

            _sentryTargeting.ToggleFaction((sentryUid, targeting), args.Faction, args.Targeted);
        }

        UpdateUI(laptop);
    }

    private void OnGlobalResetTargetingMsg(Entity<SentryLaptopComponent> laptop, ref SentryLaptopGlobalResetTargetingBuiMsg args)
    {
        foreach (var sentryUid in laptop.Comp.LinkedSentries)
        {
            if (!TryComp<SentryTargetingComponent>(sentryUid, out var targeting))
                continue;

            _sentryTargeting.ResetToDefault((sentryUid, targeting));
        }

        UpdateUI(laptop);
    }

    private void OnGlobalTogglePowerMsg(Entity<SentryLaptopComponent> laptop, ref SentryLaptopGlobalTogglePowerBuiMsg args)
    {
        var newMode = args.PowerOn ? SentryMode.On : SentryMode.Off;

        foreach (var sentryUid in laptop.Comp.LinkedSentries)
        {
            if (!TryComp<SentryComponent>(sentryUid, out var sentry))
                continue;

            if (sentry.Mode == SentryMode.Item)
                continue;

            EntityManager.System<SentrySystem>().TrySetMode((sentryUid, sentry), newMode);
        }

        UpdateUI(laptop);
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

    protected override void UpdateUI(Entity<SentryLaptopComponent> laptop)
    {
        if (!_ui.IsUiOpen(laptop.Owner, SentryLaptopUiKey.Key))
            return;

        var sentries = BuildSentryInfoList(laptop);
        var state = new SentryLaptopBuiState(sentries);
        _ui.SetUiState(laptop.Owner, SentryLaptopUiKey.Key, state);
    }

    private List<SentryInfo> BuildSentryInfoList(Entity<SentryLaptopComponent> laptop)
    {
        var sentries = new List<SentryInfo>();

        foreach (var sentryUid in laptop.Comp.LinkedSentries)
        {
            if (!TryComp<SentryComponent>(sentryUid, out var sentry))
                continue;

            var info = BuildSentryInfo((laptop.Owner, laptop.Comp), sentryUid, sentry);
            sentries.Add(info);
        }

        return sentries;
    }

    private SentryInfo BuildSentryInfo(Entity<SentryLaptopComponent> laptop, EntityUid sentryUid, SentryComponent sentry)
    {
        var displayName = GetSentryDisplayName(laptop, sentryUid);
        var customName = laptop.Comp.SentryCustomNames.TryGetValue(sentryUid, out var cName) ? cName : null;
        var visionRadius = GetSentryVisionRadius(sentryUid);
        var maxDeviation = (float)sentry.MaxDeviation.Degrees;

        return new SentryInfo(
            GetNetEntity(sentryUid),
            displayName,
            sentry.Mode,
            GetSentryHealth(sentryUid, out var maxHealth),
            maxHealth,
            GetSentryAmmo(sentryUid, out var maxAmmo),
            maxAmmo,
            GetSentryLocation(sentryUid),
            GetSentryTarget(sentryUid),
            GetSentryFriendlyFactions(sentryUid),
            customName,
            visionRadius,
            maxDeviation
        );
    }

    private float GetSentryVisionRadius(EntityUid sentry)
    {
        if (!TryComp<HTNComponent>(sentry, out var htn))
            return 5.0f;

        if (htn.Blackboard.TryGetValue<float>("VisionRadius", out var radius, EntityManager))
            return radius;

        return 5.0f;
    }

    private string GetSentryDisplayName(Entity<SentryLaptopComponent> laptop, EntityUid sentry)
    {
        if (laptop.Comp.SentryCustomNames.TryGetValue(sentry, out var customName))
            return customName;

        return Name(sentry);
    }

    private float GetSentryHealth(EntityUid sentry, out float maxHealth)
    {
        maxHealth = 100f;
        var health = 0f;

        if (TryComp<DamageableComponent>(sentry, out var damageable) &&
            _destructible.DestroyedAt(sentry) is { } destroyThreshold)
        {
            maxHealth = destroyThreshold.Float();
            health = Math.Max(0, maxHealth - damageable.TotalDamage.Float());
        }

        return health;
    }

    private int GetSentryAmmo(EntityUid sentry, out int maxAmmo)
    {
        maxAmmo = 0;
        var ammo = 0;

        if (TryComp<GunComponent>(sentry, out var gun))
        {
            if (TryComp<ContainerManagerComponent>(sentry, out var container))
            {
                foreach (var cont in container.Containers.Values)
                {
                    foreach (var containedEntity in cont.ContainedEntities)
                    {
                        if (TryComp<BallisticAmmoProviderComponent>(containedEntity, out var ammoProvider))
                        {
                            ammo = ammoProvider.Count;
                            maxAmmo = ammoProvider.Capacity;
                            return ammo;
                        }
                    }
                }
            }
        }

        if (TryComp<BallisticAmmoProviderComponent>(sentry, out var directAmmo))
        {
            ammo = directAmmo.Count;
            maxAmmo = directAmmo.Capacity;
        }

        return ammo;
    }

    private string GetSentryLocation(EntityUid sentry)
    {
        if (_area.TryGetArea(sentry, out var area, out _))
            return Name(area.Value);

        return "Unknown";
    }

    private NetEntity? GetSentryTarget(EntityUid sentry)
    {
        if (TryComp<GunComponent>(sentry, out var gun) && gun.Target != null)
            return GetNetEntity(gun.Target.Value);

        return null;
    }

    private HashSet<string> GetSentryFriendlyFactions(EntityUid sentry)
    {
        if (TryComp<SentryTargetingComponent>(sentry, out var targeting))
            return targeting.FriendlyFactions;

        return new HashSet<string> { "UNMC" };
    }
}
