using System;
using System.Collections.Generic;
using System.Linq;
using Content.Shared._RMC14.Areas;
using Content.Shared._RMC14.Tools;
using Content.Shared._RMC14.Weapons.Ranged.IFF;
using Content.Shared.Damage;
using Content.Shared.Containers.ItemSlots;
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

    private const float UpdateInterval = 1.0f;

    private float _updateTimer;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SentryLaptopComponent, AfterInteractEvent>(OnLaptopAfterInteract);
        SubscribeLocalEvent<SentryLaptopComponent, ItemToggledEvent>(OnLaptopToggled);
        SubscribeLocalEvent<SentryLaptopComponent, ComponentShutdown>(OnLaptopShutdown);
        SubscribeLocalEvent<SentryLaptopComponent, ActivatableUIOpenAttemptEvent>(OnLaptopUIOpenAttempt);

        SubscribeLocalEvent<SentryLaptopLinkedComponent, ComponentShutdown>(OnSentryLinkedShutdown);

        SubscribeLocalEvent<SentryComponent, AfterInteractUsingEvent>(OnSentryInteractUsing);
        SubscribeLocalEvent<SentryLaptopComponent, AfterInteractUsingEvent>(OnLaptopInteractUsing);

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

            foreach (var sentryUid in laptop.LinkedSentries)
            {
                if (!TryComp<SentryComponent>(sentryUid, out var sentry))
                    continue;

                var ammo = GetSentryAmmo(sentryUid, out var maxAmmo);
                if (maxAmmo > 0 && (float) ammo / maxAmmo <= sentry.LowAmmoThreshold)
                {
                    if (time - sentry.LastLowAmmoAlert > sentry.AlertCooldown)
                    {
                        sentry.LastLowAmmoAlert = time;
                        Dirty(sentryUid, sentry);

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
                        Dirty(sentryUid, sentry);

                        SendAlert(laptopUid, sentryUid, SentryAlertType.CriticalHealth,
                            $"{GetSentryDisplayName((laptopUid, laptop), sentryUid)}: CRITICAL DAMAGE");
                    }
                }
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

        if (!TryComp<SentryLaptopLinkedComponent>(sentry, out var linked) || linked.LinkedLaptop == null)
            return;

        if (!TryComp<SentryLaptopComponent>(linked.LinkedLaptop.Value, out var laptop))
            return;

        var health = GetSentryHealth(sentry, out var maxHealth);
        var healthPercent = maxHealth > 0 ? (int) ((health / maxHealth) * 100) : 0;

        SendAlert(linked.LinkedLaptop.Value, sentry, SentryAlertType.Damaged,
            $"{GetSentryDisplayName((linked.LinkedLaptop.Value, laptop), sentry)}: Taking damage! ({healthPercent}% health)");
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
        if (!_net.IsServer)
            return;

        if (!_ui.IsUiOpen(laptop, SentryLaptopUiKey.Key))
            return;

        var alert = new SentryAlertEvent(GetNetEntity(sentry), alertType, message);
        _ui.ServerSendUiMessage(laptop, SentryLaptopUiKey.Key, alert);
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

    private void OnLaptopToggled(Entity<SentryLaptopComponent> laptop, ref ItemToggledEvent args)
    {
        laptop.Comp.IsOpen = args.Activated;

        if (!args.Activated)
        {
            SetPowered(laptop, false);

            if (_net.IsServer)
                _ui.CloseUi(laptop.Owner, SentryLaptopUiKey.Key);
        }

        UpdateLaptopVisuals(laptop);
        Dirty(laptop);
    }

    private void OnLaptopUIOpenAttempt(Entity<SentryLaptopComponent> laptop, ref ActivatableUIOpenAttemptEvent args)
    {
        if (laptop.Comp.IsOpen)
            return;

        _popup.PopupClient("The laptop must be opened first!", laptop, args.User);
        args.Cancel();
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

    private void OnSentryInteractUsing(Entity<SentryComponent> sentry, ref AfterInteractUsingEvent args)
    {
        if (args.Handled || !args.CanReach)
            return;

        if (!HasComp<MultitoolComponent>(args.Used))
            return;

        if (!TryComp<NetworkConfiguratorComponent>(args.Used, out var configurator))
            return;

        args.Handled = true;

        if (IsSentryAlreadyLinked(sentry))
        {
            _popup.PopupClient("This sentry is already linked to a laptop!", sentry, args.User);
            return;
        }

        HandleLinkInteraction(args.Used, sentry.Owner, isLaptopTarget: false, args.User, ref configurator);
    }

    private void OnLaptopInteractUsing(Entity<SentryLaptopComponent> laptop, ref AfterInteractUsingEvent args)
    {
        if (args.Handled || !args.CanReach)
            return;

        if (!HasComp<MultitoolComponent>(args.Used))
            return;

        if (!TryComp<NetworkConfiguratorComponent>(args.Used, out var configurator))
            return;

        args.Handled = true;

        HandleLinkInteraction(args.Used, laptop.Owner, isLaptopTarget: true, args.User, ref configurator);
    }

    private void HandleLinkInteraction(EntityUid multitool, EntityUid target, bool isLaptopTarget, EntityUid user, ref NetworkConfiguratorComponent configurator)
    {
        if (configurator.ActiveDeviceLink == null)
        {
            configurator.LinkModeActive = true;
            configurator.ActiveDeviceLink = target;
            configurator.DeviceLinkTarget = null;
            Dirty(multitool, configurator);

            var targetName = Name(target);
            _popup.PopupClient($"Link mode started: {targetName} stored.", target, user);
            return;
        }

        var stored = configurator.ActiveDeviceLink;
        var storedIsLaptop = stored.HasValue && HasComp<SentryLaptopComponent>(stored.Value);
        var storedIsSentry = stored.HasValue && HasComp<SentryComponent>(stored.Value);

        if (storedIsLaptop == isLaptopTarget)
        {
            configurator.ActiveDeviceLink = null;
            configurator.DeviceLinkTarget = null;
            Dirty(multitool, configurator);
            _popup.PopupClient("Link mode stopped.", target, user);
            return;
        }

        if (!stored.HasValue || !TryGetEntity(GetNetEntity(stored.Value), out var storedEnt))
        {
            configurator.ActiveDeviceLink = null;
            configurator.DeviceLinkTarget = null;
            Dirty(multitool, configurator);
            _popup.PopupClient("Stored link target is missing.", target, user);
            return;
        }

        Entity<SentryLaptopComponent>? laptopEntity = null;
        Entity<SentryComponent>? sentryEntity = null;

        if (storedIsLaptop &&
            TryComp<SentryLaptopComponent>(storedEnt.Value, out var storedLaptop) &&
            TryComp<SentryComponent>(target, out var targetSentry))
        {
            laptopEntity = new Entity<SentryLaptopComponent>(storedEnt.Value, storedLaptop);
            sentryEntity = new Entity<SentryComponent>(target, targetSentry);
        }
        else if (storedIsSentry &&
                 TryComp<SentryComponent>(storedEnt.Value, out var storedSentry) &&
                 TryComp<SentryLaptopComponent>(target, out var targetLaptop))
        {
            laptopEntity = new Entity<SentryLaptopComponent>(target, targetLaptop);
            sentryEntity = new Entity<SentryComponent>(storedEnt.Value, storedSentry);
        }
        else
        {
            configurator.ActiveDeviceLink = null;
            configurator.DeviceLinkTarget = null;
            Dirty(multitool, configurator);
            _popup.PopupClient("Invalid link targets.", target, user);
            return;
        }

        if (laptopEntity is null || sentryEntity is null)
        {
            configurator.ActiveDeviceLink = null;
            configurator.DeviceLinkTarget = null;
            Dirty(multitool, configurator);
            _popup.PopupClient("Stored link target is unavailable.", target, user);
            return;
        }

        if (IsSentryAlreadyLinked(sentryEntity.Value))
        {
            _popup.PopupClient("This sentry is already linked to a laptop!", sentryEntity.Value, user);
            configurator.ActiveDeviceLink = null;
            configurator.DeviceLinkTarget = null;
            Dirty(multitool, configurator);
            return;
        }

        if (!ValidateLaptopForLinking(laptopEntity.Value, sentryEntity.Value, user))
        {
            configurator.ActiveDeviceLink = null;
            configurator.DeviceLinkTarget = null;
            Dirty(multitool, configurator);
            return;
        }

        if (_net.IsServer)
            LinkSentryToLaptop(laptopEntity.Value, sentryEntity.Value, user);

        configurator.ActiveDeviceLink = null;
        configurator.DeviceLinkTarget = null;
        Dirty(multitool, configurator);
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

        if (!laptop.Comp.LinkedSentries.Contains(sentryEnt.Value))
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

        if (!laptop.Comp.LinkedSentries.Contains(sentryEnt.Value))
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

        if (!laptop.Comp.LinkedSentries.Contains(sentryEnt.Value))
            return;

        if (!TryComp<SentryComponent>(sentryEnt.Value, out var sentry))
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

        if (!laptop.Comp.LinkedSentries.Contains(sentryEnt.Value))
            return;

        if (!TryComp<SentryTargetingComponent>(sentryEnt.Value, out var targeting))
            return;

        _sentryTargeting.ResetToDefault((sentryEnt.Value, targeting));
        UpdateUI(laptop);
    }

    private void PlaceLaptopOnSurface(Entity<SentryLaptopComponent> laptop, EntityUid surface, EntityUid user)
    {
        var laptopXform = Transform(laptop);
        var surfaceXform = Transform(surface);
        laptopXform.Coordinates = surfaceXform.Coordinates;
        laptopXform.AttachParent(surface);
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

        if (laptop.Comp.LinkedSentries.Count >= laptop.Comp.MaxLinkedSentries)
        {
            _popup.PopupClient($"The laptop can only control {laptop.Comp.MaxLinkedSentries} sentries at once!", laptop, user);
            return false;
        }

        return true;
    }

    private void LinkSentryToLaptop(Entity<SentryLaptopComponent> laptop, Entity<SentryComponent> sentry, EntityUid user)
    {
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

        if (targeting.FriendlyFactions.Count == 0)
        {
            var defaultFactions = new HashSet<string>();

            if (!string.IsNullOrEmpty(targeting.OriginalFaction))
                defaultFactions.Add(targeting.OriginalFaction);
            else
                defaultFactions.Add("UNMC");

            _sentryTargeting.SetFriendlyFactions((sentry, targeting), defaultFactions);
        }
        else
        {
            _sentryTargeting.SetFriendlyFactions((sentry, targeting), new HashSet<string>(targeting.FriendlyFactions));
        }
    }

    public void UnlinkSentry(Entity<SentryLaptopComponent> laptop, EntityUid sentry)
    {
        if (!laptop.Comp.LinkedSentries.Remove(sentry))
            return;

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
        var sentries = laptop.Comp.LinkedSentries.ToList();
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

        foreach (var sentryUid in laptop.Comp.LinkedSentries)
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
        var maxDeviation = (float) sentry.MaxDeviation.Degrees;

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
