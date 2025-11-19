using Content.Shared._RMC14.Tools;
using Content.Shared._RMC14.Weapons.Ranged.IFF;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Placeable;
using Content.Shared.Popups;
using Content.Shared.Tools.Components;
using Content.Shared.UserInterface;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.Network;
using System.Linq;

namespace Content.Shared._RMC14.Sentry.Laptop;

public abstract class SharedSentryLaptopSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly ItemToggleSystem _toggle = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedSentryTargetingSystem _sentryTargeting = default!;
    [Dependency] private readonly GunIFFSystem _iff = default!;

    private const float NearbyLaptopRange = 2.0f;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SentryLaptopComponent, AfterInteractEvent>(OnLaptopAfterInteract);
        SubscribeLocalEvent<SentryLaptopComponent, ItemToggledEvent>(OnLaptopToggled);
        SubscribeLocalEvent<SentryLaptopComponent, ComponentShutdown>(OnLaptopShutdown);
        SubscribeLocalEvent<SentryLaptopComponent, ActivatableUIOpenAttemptEvent>(OnLaptopUIOpenAttempt);

        SubscribeLocalEvent<SentryLaptopLinkedComponent, ComponentShutdown>(OnSentryLinkedShutdown);

        SubscribeLocalEvent<SentryComponent, AfterInteractUsingEvent>(OnSentryInteractUsing);

        SubscribeLocalEvent<SentryLaptopComponent, SentryLaptopUnlinkBuiMsg>(OnUnlinkMessage);
        SubscribeLocalEvent<SentryLaptopComponent, SentryLaptopUnlinkAllBuiMsg>(OnUnlinkAllMessage);
        SubscribeLocalEvent<SentryLaptopComponent, SentryLaptopSetFactionsBuiMsg>(OnSetFactionsMessage);
        SubscribeLocalEvent<SentryLaptopComponent, SentryLaptopToggleFactionBuiMsg>(OnToggleFactionMessage);
        SubscribeLocalEvent<SentryLaptopComponent, SentryLaptopResetTargetingBuiMsg>(OnResetTargetingMessage);
        SubscribeLocalEvent<SentryLaptopComponent, SentryLaptopTogglePowerBuiMsg>(OnTogglePowerMessage);
    }

    private void OnLaptopAfterInteract(Entity<SentryLaptopComponent> laptop, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target == null)
            return;

        if (!HasComp<PlaceableSurfaceComponent>(args.Target.Value))
            return;

        args.Handled = true;

        if (_net.IsServer)
            PlaceLaptopOnSurface(laptop, args.Target.Value);
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
        if (!laptop.Comp.IsOpen)
        {
            _popup.PopupClient("The laptop must be opened first!", laptop, args.User);
            args.Cancel();
        }
    }

    private void OnLaptopShutdown(Entity<SentryLaptopComponent> laptop, ref ComponentShutdown args)
    {
        UnlinkAllSentries(laptop);
    }

    private void OnSentryLinkedShutdown(Entity<SentryLaptopLinkedComponent> sentry, ref ComponentShutdown args)
    {
        if (sentry.Comp.LinkedLaptop != null && TryComp<SentryLaptopComponent>(sentry.Comp.LinkedLaptop.Value, out var laptop))
        {
            UnlinkSentry((sentry.Comp.LinkedLaptop.Value, laptop), sentry.Owner);
        }
    }

    private void OnSentryInteractUsing(Entity<SentryComponent> sentry, ref AfterInteractUsingEvent args)
    {
        if (args.Handled || !args.CanReach)
            return;

        if (!HasComp<MultitoolComponent>(args.Used))
            return;

        args.Handled = true;

        if (IsSentryAlreadyLinked(sentry))
        {
            _popup.PopupClient("This sentry is already linked to a laptop!", sentry, args.User);
            return;
        }

        var laptop = FindLaptopForLinking(sentry, args.User);
        if (laptop == null)
        {
            _popup.PopupClient("No laptop found nearby or in inventory!", sentry, args.User);
            return;
        }

        if (!ValidateLaptopForLinking(laptop.Value, sentry, args.User))
            return;

        if (_net.IsServer)
            LinkSentryToLaptop(laptop.Value, sentry, args.User);
    }

    private void OnUnlinkMessage(Entity<SentryLaptopComponent> laptop, ref SentryLaptopUnlinkBuiMsg args)
    {
        if (!_net.IsServer || !TryGetEntity(args.Sentry, out var sentryEnt))
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
        if (!_net.IsServer || !TryGetEntity(args.Sentry, out var sentryEnt))
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
        if (!_net.IsServer || !TryGetEntity(args.Sentry, out var sentryEnt))
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
        if (!_net.IsServer || !TryGetEntity(args.Sentry, out var sentryEnt))
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
        if (!_net.IsServer || !TryGetEntity(args.Sentry, out var sentryEnt))
            return;

        if (!laptop.Comp.LinkedSentries.Contains(sentryEnt.Value))
            return;

        if (!TryComp<SentryTargetingComponent>(sentryEnt.Value, out var targeting))
            return;

        _sentryTargeting.ResetToDefault((sentryEnt.Value, targeting));
        UpdateUI(laptop);
    }

    private void PlaceLaptopOnSurface(Entity<SentryLaptopComponent> laptop, EntityUid surface)
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

    private Entity<SentryLaptopComponent>? FindLaptopForLinking(Entity<SentryComponent> sentry, EntityUid user)
    {
        var laptop = FindLaptopInInventory(user);
        if (laptop != null)
            return laptop;

        return FindLaptopNearby(sentry);
    }

    private Entity<SentryLaptopComponent>? FindLaptopInInventory(EntityUid user)
    {
        if (_inventory.TryGetSlotEntity(user, "belt", out var beltItem) && TryComp<SentryLaptopComponent>(beltItem, out var laptop))
            return new Entity<SentryLaptopComponent>(beltItem.Value, laptop);

        if (_inventory.TryGetSlotEntity(user, "back", out var backItem) && TryComp<SentryLaptopComponent>(backItem, out laptop))
            return new Entity<SentryLaptopComponent>(backItem.Value, laptop);

        return null;
    }

    private Entity<SentryLaptopComponent>? FindLaptopNearby(Entity<SentryComponent> sentry)
    {
        var sentryPos = _transform.GetMapCoordinates(sentry);
        var query = EntityQueryEnumerator<SentryLaptopComponent, TransformComponent>();

        while (query.MoveNext(out var uid, out var laptop, out var xform))
        {
            var laptopPos = _transform.GetMapCoordinates(uid, xform);
            if (laptopPos.MapId != sentryPos.MapId)
                continue;

            var distance = (laptopPos.Position - sentryPos.Position).Length();
            if (distance <= NearbyLaptopRange)
                return (uid, laptop);
        }

        return null;
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

        InitializeSentryTargeting(sentry);

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
        {
            _iff.EnableIntrinsicIFF(sentry);
        }

        if (targeting.FriendlyFactions.Count == 0)
        {
            var defaultFactions = new HashSet<string>();

            if (!string.IsNullOrEmpty(targeting.OriginalFaction))
            {
                defaultFactions.Add(targeting.OriginalFaction);
            }
            else
            {
                defaultFactions.Add("UNMC");
            }

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
        {
            UnlinkSentry(laptop, sentry);
        }
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
}
