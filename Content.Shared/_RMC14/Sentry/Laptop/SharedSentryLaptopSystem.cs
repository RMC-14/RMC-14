using System;
using System.Collections.Generic;
using System.Linq;
using Content.Shared._RMC14.Areas;
using Content.Shared._RMC14.Tools;
using Content.Shared._RMC14.Weapons.Ranged.IFF;
using Content.Shared.Damage;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.DeviceLinking;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Placeable;
using Content.Shared.Popups;
using Content.Shared.Tools.Components;
using Content.Shared.UserInterface;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Systems;
using Content.Shared.DeviceNetwork.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Timing;
using SentryAlertEvent = Content.Shared._RMC14.Sentry.Laptop.SentryAlertEvent;
using SentryAlertType = Content.Shared._RMC14.Sentry.Laptop.SentryAlertType;

namespace Content.Shared._RMC14.Sentry.Laptop;

public abstract class SharedSentryLaptopSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly ItemToggleSystem _toggle = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedSentryTargetingSystem _sentryTargeting = default!;
    [Dependency] private readonly GunIFFSystem _iff = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly AreaSystem _area = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedContainerSystem _containers = default!;
    [Dependency] private readonly SharedDeviceLinkSystem _deviceLink = default!;

    private const float UpdateInterval = 1.0f;

    private float _updateTimer;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SentryLaptopComponent, AfterInteractEvent>(OnLaptopAfterInteract);
        SubscribeLocalEvent<SentryLaptopComponent, ComponentShutdown>(OnLaptopShutdown);
        SubscribeLocalEvent<SentryLaptopComponent, ActivatableUIOpenAttemptEvent>(OnLaptopUIOpenAttempt);
        SubscribeLocalEvent<SentryLaptopComponent, EntParentChangedMessage>(OnLaptopParentChanged);

        SubscribeLocalEvent<SentryLaptopLinkedComponent, ComponentShutdown>(OnSentryLinkedShutdown);

        SubscribeLocalEvent<SentryLaptopComponent, SentryLaptopUnlinkBuiMsg>(OnUnlinkMessage);
        SubscribeLocalEvent<SentryLaptopComponent, SentryLaptopUnlinkAllBuiMsg>(OnUnlinkAllMessage);
        SubscribeLocalEvent<SentryLaptopComponent, SentryLaptopSetFactionsBuiMsg>(OnSetFactionsMessage);
        SubscribeLocalEvent<SentryLaptopComponent, SentryLaptopToggleFactionBuiMsg>(OnToggleFactionMessage);
        SubscribeLocalEvent<SentryLaptopComponent, SentryLaptopResetTargetingBuiMsg>(OnResetTargetingMessage);
        SubscribeLocalEvent<SentryLaptopComponent, SentryLaptopTogglePowerBuiMsg>(OnTogglePowerMessage);

        SubscribeLocalEvent<SentryComponent, DamageChangedEvent>(OnSentryDamageChanged);
        SubscribeLocalEvent<SentryComponent, GunShotEvent>(OnSentryShot);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_net.IsServer)
            return;

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

            foreach (var sentryUid in GetLinkedSentries((laptopUid, laptop)))
            {
                if (!TryComp<SentryComponent>(sentryUid, out var sentry))
                    continue;

                CheckLowAmmoAlert(laptopUid, (sentryUid, sentry), laptop, time);
                CheckHealthAlert(laptopUid, (sentryUid, sentry), laptop, time);
            }
        }
    }

    private void CheckLowAmmoAlert(EntityUid laptopUid, Entity<SentryComponent> sentry, SentryLaptopComponent laptop, TimeSpan time)
    {
        var ammo = GetSentryAmmo(sentry, out var maxAmmo);
        if (maxAmmo > 0 && (float)ammo / maxAmmo <= sentry.Comp.LowAmmoThreshold)
        {
            if (time - sentry.Comp.LastLowAmmoAlert > sentry.Comp.AlertCooldown)
            {
                sentry.Comp.LastLowAmmoAlert = time;
                Dirty(sentry);

                SendAlert(laptopUid, sentry, SentryAlertType.LowAmmo,
                    $"{GetSentryDisplayName((laptopUid, laptop), sentry)}: LOW AMMO ({ammo}/{maxAmmo})");
            }
        }
    }

    private void CheckHealthAlert(EntityUid laptopUid, Entity<SentryComponent> sentry, SentryLaptopComponent laptop, TimeSpan time)
    {
        var health = GetSentryHealth(sentry, out var maxHealth);
        if (maxHealth > 0 && health / maxHealth <= sentry.Comp.CriticalHealthThreshold)
        {
            if (time - sentry.Comp.LastHealthAlert > sentry.Comp.AlertCooldown)
            {
                sentry.Comp.LastHealthAlert = time;
                Dirty(sentry);

                SendAlert(laptopUid, sentry, SentryAlertType.CriticalHealth,
                    $"{GetSentryDisplayName((laptopUid, laptop), sentry)}: CRITICAL DAMAGE");
            }
        }
    }

    private void OnSentryDamageChanged(Entity<SentryComponent> sentry, ref DamageChangedEvent args)
    {
        if (!_net.IsServer)
            return;

        if (!args.DamageIncreased || args.DamageDelta == null)
            return;

        var time = _timing.CurTime;
        if (time - sentry.Comp.LastHealthAlert < sentry.Comp.AlertCooldown)
            return;

        sentry.Comp.LastHealthAlert = time;
        Dirty(sentry);

        if (!TryGetLinkedLaptop(sentry.Owner, out var laptop))
            return;

        var health = GetSentryHealth(sentry, out var maxHealth);
        var healthPercent = maxHealth > 0 ? (int)((health / maxHealth) * 100) : 0;

        var laptopEntity = laptop!.Value;
        SendAlert(laptopEntity.Owner, sentry, SentryAlertType.Damaged,
            $"{GetSentryDisplayName(laptopEntity, sentry)}: Taking damage! ({healthPercent}% health)");
    }

    private void OnSentryShot(Entity<SentryComponent> sentry, ref GunShotEvent args)
    {
        if (!_net.IsServer)
            return;

        var time = _timing.CurTime;
        if (time - sentry.Comp.LastTargetAlert < sentry.Comp.AlertCooldown)
            return;

        if (!TryComp<GunComponent>(sentry, out var gun) || gun.Target == null)
            return;

        sentry.Comp.LastTargetAlert = time;
        Dirty(sentry);

        if (!TryGetLinkedLaptop(sentry.Owner, out var laptop))
            return;

        var targetName = Name(gun.Target.Value);
        var laptopEntity = laptop!.Value;
        SendAlert(laptopEntity.Owner, sentry, SentryAlertType.TargetAcquired,
            $"{GetSentryDisplayName(laptopEntity, sentry)}: Engaging {targetName}");
    }

    private void SendAlert(EntityUid laptop, EntityUid sentry, SentryAlertType alertType, string message)
    {
        if (!_net.IsServer)
            return;

        var (color, size) = GetAlertStyle(alertType);
        var alert = new SentryAlertEvent(GetNetEntity(sentry), alertType, message, color, size);

        if (_ui.IsUiOpen(laptop, SentryLaptopUiKey.Key))
        {
            _ui.ServerSendUiMessage(laptop, SentryLaptopUiKey.Key, alert);
            return;
        }

        var parent = Transform(laptop).ParentUid;
        if (!HasComp<PlaceableSurfaceComponent>(parent))
            return;

        var popupType = GetPopupType(alertType);
        _popup.PopupEntity(message, laptop, popupType);
    }

    private static (string color, int size) GetAlertStyle(SentryAlertType alertType)
    {
        return alertType switch
        {
            SentryAlertType.LowAmmo => ("#CED22B", 14),
            SentryAlertType.CriticalHealth => ("#A42625", 16),
            SentryAlertType.TargetAcquired => ("#A42625", 14),
            SentryAlertType.Damaged => ("#A42625", 14),
            _ => ("#88C7FA", 14)
        };
    }

    private static PopupType GetPopupType(SentryAlertType alertType)
    {
        return alertType switch
        {
            SentryAlertType.CriticalHealth => PopupType.LargeCaution,
            SentryAlertType.Damaged => PopupType.MediumCaution,
            SentryAlertType.TargetAcquired => PopupType.MediumCaution,
            SentryAlertType.LowAmmo => PopupType.Medium,
            _ => PopupType.Medium
        };
    }

    private void OnLaptopAfterInteract(Entity<SentryLaptopComponent> laptop, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target == null)
            return;

        if (!HasComp<PlaceableSurfaceComponent>(args.Target.Value))
            return;

        args.Handled = true;

        if (!_hands.TryDrop(args.User, laptop.Owner, checkActionBlocker: false))
            return;

        if (_net.IsServer)
            PlaceLaptopOnSurface(laptop, args.Target.Value, args.User);
    }

    private void OnLaptopUIOpenAttempt(Entity<SentryLaptopComponent> laptop, ref ActivatableUIOpenAttemptEvent args)
    {
        var parent = Transform(laptop).ParentUid;
        if (!HasComp<PlaceableSurfaceComponent>(parent))
        {
            _popup.PopupClient("Place the laptop on a table first!", laptop, args.User);
            args.Cancel();
            return;
        }

        if (!laptop.Comp.IsOpen)
        {
            laptop.Comp.IsOpen = true;
            SetPowered(laptop, true);
            UpdateLaptopVisuals(laptop);
            Dirty(laptop);
        }
    }

    private void OnLaptopShutdown(Entity<SentryLaptopComponent> laptop, ref ComponentShutdown args)
    {
        UnlinkAllSentries(laptop);
    }

    private void OnSentryLinkedShutdown(Entity<SentryLaptopLinkedComponent> sentry, ref ComponentShutdown args)
    {
        if (sentry.Comp.LinkedLaptop == null)
            return;

        if (!TryComp<SentryLaptopComponent>(sentry.Comp.LinkedLaptop.Value, out var laptop))
            return;

        UnlinkSentry((sentry.Comp.LinkedLaptop.Value, laptop), sentry.Owner);
    }

    private void OnUnlinkMessage(Entity<SentryLaptopComponent> laptop, ref SentryLaptopUnlinkBuiMsg args)
    {
        if (!_net.IsServer)
            return;

        if (!TryGetEntity(args.Sentry, out var sentryEnt))
            return;

        UnlinkSentry(laptop, sentryEnt.Value);
        UpdateUI(laptop);
    }

    private void OnUnlinkAllMessage(Entity<SentryLaptopComponent> laptop, ref SentryLaptopUnlinkAllBuiMsg args)
    {
        if (!_net.IsServer)
            return;

        UnlinkAllSentries(laptop);
        UpdateUI(laptop);
    }

    private void OnSetFactionsMessage(Entity<SentryLaptopComponent> laptop, ref SentryLaptopSetFactionsBuiMsg args)
    {
        if (!_net.IsServer)
            return;

        if (!TryGetEntity(args.Sentry, out var sentryEnt))
            return;

        if (!GetLinkedSentries(laptop).Contains(sentryEnt.Value))
            return;

        if (!TryComp<SentryTargetingComponent>(sentryEnt.Value, out var targeting))
            return;

        var factionSet = new HashSet<string>(args.Factions);
        _sentryTargeting.SetFriendlyFactions((sentryEnt.Value, targeting), factionSet);
        UpdateUI(laptop);
    }

    private void OnToggleFactionMessage(Entity<SentryLaptopComponent> laptop, ref SentryLaptopToggleFactionBuiMsg args)
    {
        if (!_net.IsServer)
            return;

        if (!TryGetEntity(args.Sentry, out var sentryEnt))
            return;

        if (!GetLinkedSentries(laptop).Contains(sentryEnt.Value))
            return;

        if (!TryComp<SentryTargetingComponent>(sentryEnt.Value, out var targeting))
            return;

        _sentryTargeting.ToggleFaction((sentryEnt.Value, targeting), args.Faction, args.Targeted);
        UpdateUI(laptop);
    }

    private void OnTogglePowerMessage(Entity<SentryLaptopComponent> laptop, ref SentryLaptopTogglePowerBuiMsg args)
    {
        if (!_net.IsServer)
            return;

        if (!TryGetEntity(args.Sentry, out var sentryEnt))
            return;

        if (!GetLinkedSentries(laptop).Contains(sentryEnt.Value))
            return;

        if (!TryComp<SentryComponent>(sentryEnt.Value, out var sentry))
            return;

        if (sentry.Mode == SentryMode.Item)
            return;

        var newMode = sentry.Mode == SentryMode.On ? SentryMode.Off : SentryMode.On;
        EntityManager.System<SentrySystem>().TrySetMode((sentryEnt.Value, sentry), newMode);
        UpdateUI(laptop);
    }

    private void OnResetTargetingMessage(Entity<SentryLaptopComponent> laptop, ref SentryLaptopResetTargetingBuiMsg args)
    {
        if (!_net.IsServer)
            return;

        if (!TryGetEntity(args.Sentry, out var sentryEnt))
            return;

        if (!GetLinkedSentries(laptop).Contains(sentryEnt.Value))
            return;

        if (!TryComp<SentryTargetingComponent>(sentryEnt.Value, out var targeting))
            return;

        _sentryTargeting.ResetToDefault((sentryEnt.Value, targeting));
        UpdateUI(laptop);
    }

    private void PlaceLaptopOnSurface(Entity<SentryLaptopComponent> laptop, EntityUid surface, EntityUid user)
    {
        var surfaceXform = Transform(surface);

        _transform.SetCoordinates(laptop.Owner, surfaceXform.Coordinates);
        _transform.SetParent(laptop.Owner, surface);

        laptop.Comp.IsOpen = true;
        SetPowered(laptop, true);
        UpdateLaptopVisuals(laptop);
        Dirty(laptop);
    }

    private void OnLaptopParentChanged(Entity<SentryLaptopComponent> laptop, ref EntParentChangedMessage args)
    {
        var parent = Transform(laptop).ParentUid;
        var onSurface = HasComp<PlaceableSurfaceComponent>(parent);

        laptop.Comp.IsOpen = onSurface;
        SetPowered(laptop, onSurface);
        UpdateLaptopVisuals(laptop);
        Dirty(laptop);

        if (_net.IsServer && !onSurface)
            _ui.CloseUi(laptop.Owner, SentryLaptopUiKey.Key);
    }

    private bool IsSentryAlreadyLinked(Entity<SentryComponent> sentry)
    {
        return TryComp<SentryLaptopLinkedComponent>(sentry, out var linked) && linked.LinkedLaptop != null;
    }

    private bool ValidateLaptopForLinking(Entity<SentryLaptopComponent> laptop, Entity<SentryComponent> sentry, EntityUid user)
    {
        if (!laptop.Comp.IsOpen)
        {
            _popup.PopupClient("The laptop must be opened first!", laptop, user);
            return false;
        }

        if (GetLinkedSentries(laptop).Count >= laptop.Comp.MaxLinkedSentries)
        {
            _popup.PopupClient($"The laptop can only control {laptop.Comp.MaxLinkedSentries} sentries at once!", laptop, user);
            return false;
        }

        return true;
    }

    private void LinkSentryToLaptop(Entity<SentryLaptopComponent> laptop, Entity<SentryComponent> sentry, EntityUid user)
    {
        var source = EnsureComp<DeviceLinkSourceComponent>(laptop.Owner);
        var sink = EnsureComp<DeviceLinkSinkComponent>(sentry.Owner);

        _deviceLink.LinkDefaults(user, laptop.Owner, sentry.Owner, source, sink);

        laptop.Comp.LinkedSentries.Add(sentry);

        var linkedComp = EnsureComp<SentryLaptopLinkedComponent>(sentry);
        linkedComp.LinkedLaptop = laptop;
        Dirty(sentry, linkedComp);

        InitializeSentryTargeting(sentry.Owner);

        _popup.PopupEntity($"Successfully linked {Name(sentry)} to the laptop.", sentry, user);

        if (laptop.Comp.LinkedSentries.Count == 1)
            SetPowered(laptop, true);

        Dirty(laptop);
        UpdateUI(laptop);
    }

    private void InitializeSentryTargeting(EntityUid sentry)
    {
        if (!TryComp<SentryTargetingComponent>(sentry, out var targeting))
            targeting = EnsureComp<SentryTargetingComponent>(sentry);

        if (!HasComp<GunIFFComponent>(sentry) && HasComp<GunComponent>(sentry))
            _iff.EnableIntrinsicIFF(sentry);

        var defaultFactions = targeting.FriendlyFactions.Count > 0
            ? new HashSet<string>(targeting.FriendlyFactions)
            : new HashSet<string>();

        if (defaultFactions.Count == 0)
        {
            if (!string.IsNullOrEmpty(targeting.OriginalFaction))
                defaultFactions.Add(targeting.OriginalFaction);
            else
                defaultFactions.Add("UNMC");
        }

        foreach (var faction in _sentryTargeting.GetHumanoidFactions())
            defaultFactions.Add(faction);

        _sentryTargeting.SetFriendlyFactions((sentry, targeting), defaultFactions);
    }

    public void UnlinkSentry(Entity<SentryLaptopComponent> laptop, EntityUid sentry)
    {
        if (!laptop.Comp.LinkedSentries.Remove(sentry))
            return;

        if (TryComp<DeviceLinkSinkComponent>(sentry, out var sink))
            _deviceLink.RemoveAllFromSink(sentry, sink);

        laptop.Comp.SentryCustomNames.Remove(sentry);
        RemComp<SentryLaptopLinkedComponent>(sentry);

        if (TryComp<SentryTargetingComponent>(sentry, out var targeting))
            _sentryTargeting.ResetToDefault((sentry, targeting));

        if (laptop.Comp.LinkedSentries.Count == 0)
            SetPowered(laptop, false);

        Dirty(laptop);
    }

    private void UnlinkAllSentries(Entity<SentryLaptopComponent> laptop)
    {
        var sentries = GetLinkedSentries(laptop).ToList();
        foreach (var sentry in sentries)
            UnlinkSentry(laptop, sentry);
    }

    public void SetPowered(Entity<SentryLaptopComponent> laptop, bool powered)
    {
        laptop.Comp.IsPowered = powered;
        UpdateLaptopVisuals(laptop);
        Dirty(laptop);
    }

    private void UpdateLaptopVisuals(Entity<SentryLaptopComponent> laptop)
    {
        var state = SentryLaptopState.Closed;

        if (laptop.Comp.IsOpen)
            state = laptop.Comp.IsPowered ? SentryLaptopState.Active : SentryLaptopState.Open;

        _appearance.SetData(laptop, SentryLaptopVisuals.State, state);
    }

    protected virtual void UpdateUI(Entity<SentryLaptopComponent> laptop)
    {
    }

    protected List<SentryInfo> BuildSentryInfoList(Entity<SentryLaptopComponent> laptop)
    {
        var sentries = new List<SentryInfo>();

        foreach (var sentryUid in GetLinkedSentries(laptop))
        {
            if (!TryComp<SentryComponent>(sentryUid, out var sentry))
                continue;

            var info = BuildSentryInfo(laptop, sentryUid, sentry);
            sentries.Add(info);
        }

        return sentries;
    }

    protected SentryInfo BuildSentryInfo(Entity<SentryLaptopComponent> laptop, EntityUid sentryUid, SentryComponent sentry)
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
            maxDeviation,
            GetSentryHumanoidAdded(sentryUid)
        );
    }

    protected virtual float GetSentryVisionRadius(EntityUid sentry)
    {
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
        maxHealth = GetSentryMaxHealth(sentry);
        var health = maxHealth;

        if (TryComp<DamageableComponent>(sentry, out var damageable))
        {
            var damage = damageable.TotalDamage.Float();
            health = Math.Max(0, maxHealth - damage);
        }

        return health;
    }

    private float GetSentryHealth(Entity<SentryComponent> sentry, out float maxHealth)
    {
        return GetSentryHealth(sentry.Owner, out maxHealth);
    }

    private int GetSentryAmmo(EntityUid sentry, out int maxAmmo)
    {
        maxAmmo = 0;

        if (!EntityManager.System<SentrySystem>()
            .TryGetSentryAmmo(sentry, out var ammoCount, out var ammoCapacity))
            return 0;

        maxAmmo = ammoCapacity.Value;
        return ammoCount.Value;
    }

    private string GetSentryLocation(EntityUid sentry)
    {
        if (_area.TryGetArea(sentry, out var area, out _))
            return Name(area.Value);

        return "Unknown";
    }

    protected List<EntityUid> GetLinkedSentries(Entity<SentryLaptopComponent> laptop)
    {
        var linked = new List<EntityUid>();

        if (TryComp<DeviceLinkSourceComponent>(laptop, out var source))
        {
            foreach (var sink in source.LinkedPorts.Keys)
            {
                if (HasComp<SentryComponent>(sink))
                    linked.Add(sink);
            }

            laptop.Comp.LinkedSentries = linked.ToHashSet();
            return linked;
        }

        linked.AddRange(laptop.Comp.LinkedSentries);
        return linked;
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

        return new HashSet<string>();
    }

    private HashSet<string> GetSentryHumanoidAdded(EntityUid sentry)
    {
        if (TryComp<SentryTargetingComponent>(sentry, out var targeting))
            return targeting.HumanoidAdded;

        return new HashSet<string>();
    }

    protected virtual float GetSentryMaxHealth(EntityUid sentry)
    {
        return 100f;
    }

    private bool TryGetLinkedLaptop(EntityUid sentry, out Entity<SentryLaptopComponent>? laptop)
    {
        laptop = null;

        if (!TryComp<DeviceLinkSinkComponent>(sentry, out var sink))
            return false;

        foreach (var source in sink.LinkedSources)
        {
            if (TryComp<SentryLaptopComponent>(source, out var comp))
            {
                laptop = new Entity<SentryLaptopComponent>(source, comp);
                return true;
            }
        }

        return false;
    }
}
