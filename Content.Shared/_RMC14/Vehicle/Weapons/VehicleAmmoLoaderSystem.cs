using System;
using System.Collections.Generic;
using Content.Shared._RMC14.Weapons.Ranged.Ammo.BulletBox;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.DoAfter;
using Content.Shared.Hands;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Content.Shared.UserInterface;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Vehicle;

public sealed class VehicleAmmoLoaderSystem : EntitySystem
{
    [Dependency] private readonly BulletBoxSystem _bulletBox = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly VehicleHardpointAmmoSystem _hardpointAmmo = default!;
    [Dependency] private readonly VehicleSystem _vehicleSystem = default!;
    [Dependency] private readonly VehicleTopologySystem _topology = default!;

    private readonly Dictionary<EntityUid, Dictionary<EntityUid, EntityUid>> _activeAmmoBoxes = new();
    private readonly Dictionary<EntityUid, HashSet<EntityUid>> _openLoadersByUser = new();

    public override void Initialize()
    {
        SubscribeLocalEvent<VehicleAmmoLoaderComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<VehicleAmmoLoaderComponent, InteractHandEvent>(OnInteractHand);
        SubscribeLocalEvent<VehicleAmmoLoaderComponent, VehicleAmmoLoaderDoAfterEvent>(OnLoadDoAfter);
        SubscribeLocalEvent<VehicleAmmoLoaderComponent, BoundUIOpenedEvent>(OnUiOpened);
        SubscribeLocalEvent<VehicleAmmoLoaderComponent, BoundUIClosedEvent>(OnUiClosed);
        SubscribeLocalEvent<VehicleAmmoLoaderComponent, VehicleAmmoLoaderSelectMessage>(OnUiSelect);
        SubscribeLocalEvent<HandsComponent, DidEquipHandEvent>(OnHandItemChanged);
        SubscribeLocalEvent<HandsComponent, DidUnequipHandEvent>(OnHandItemChanged);
        SubscribeLocalEvent<BulletBoxComponent, HandSelectedEvent>(OnBulletBoxHandSelected);
        SubscribeLocalEvent<BulletBoxComponent, HandDeselectedEvent>(OnBulletBoxHandDeselected);
        SubscribeLocalEvent<VehicleHardpointAmmoComponent, VehicleAmmoChangedEvent>(OnVehicleAmmoChanged);
        SubscribeLocalEvent<HardpointSlotsChangedEvent>(OnHardpointSlotsChanged);
    }

    private void OnInteractUsing(Entity<VehicleAmmoLoaderComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled || _net.IsClient)
            return;

        if (!TryComp(args.Used, out BulletBoxComponent? _))
            return;

        TrySetActiveAmmoBox(ent.Owner, args.User, args.Used);

        if (!TryOpenUi(ent, args.User))
            return;

        args.Handled = true;
    }

    private void OnInteractHand(Entity<VehicleAmmoLoaderComponent> ent, ref InteractHandEvent args)
    {
        if (args.Handled || _net.IsClient)
            return;

        if (!TryOpenUi(ent, args.User))
            return;

        args.Handled = true;
    }

    private bool TryOpenUi(Entity<VehicleAmmoLoaderComponent> ent, EntityUid user)
    {
        if (!TryComp(ent.Owner, out UserInterfaceComponent? uiComp) ||
            !_ui.HasUi(ent.Owner, VehicleAmmoLoaderUiKey.Key, uiComp))
        {
            return false;
        }

        if (!_vehicleSystem.TryGetVehicleFromInterior(ent.Owner, out var vehicleUid) || vehicleUid == null)
        {
            _popup.PopupClient(Loc.GetString("rmc-vehicle-ammo-loader-no-vehicle"), ent, user);
            return false;
        }

        if (!TryComp(vehicleUid.Value, out HardpointSlotsComponent? hardpoints) ||
            !TryComp(vehicleUid.Value, out ItemSlotsComponent? itemSlots))
        {
            _popup.PopupClient(Loc.GetString("rmc-vehicle-ammo-loader-no-hardpoint"), ent, user);
            return false;
        }

        if (!CanOpenUi(ent.Owner, user))
            return false;

        TrySetActiveAmmoBoxFromHeld(ent, user);

        _ui.OpenUi(ent.Owner, VehicleAmmoLoaderUiKey.Key, user);
        UpdateUi(ent.Owner, user);
        return true;
    }

    private void OnLoadDoAfter(Entity<VehicleAmmoLoaderComponent> ent, ref VehicleAmmoLoaderDoAfterEvent args)
    {
        if (_net.IsClient || args.Cancelled || args.Handled)
            return;

        if (!args.SlotPath.IsValid)
            return;

        if (args.Action == VehicleAmmoLoaderSlotAction.Unload)
        {
            if (!TryGetUnloadableAmmoProvider(ent, args.User, args.SlotPath, out _, out var directAmmoUid, out var directAmmo, out var directHardpointAmmo, out var refill))
                return;

            DoDirectUnloadAmmoSlot(ent, args.User, directAmmoUid, directAmmo, directHardpointAmmo, refill, args.AmmoSlot);
            UpdateUi(ent.Owner, args.User);
            args.Handled = true;
            return;
        }

        if (args.Used is not { } used || !TryComp(used, out BulletBoxComponent? box))
            return;

        if (!TryGetActiveAmmoBox(ent.Owner, args.User, out var activeBox) || activeBox != used)
            return;

        if (!TryGetLoadableAmmoProvider(ent, args.User, box, args.SlotPath, out _, out var ammoUid, out var ammo, out var hardpointAmmo))
            return;

        if (args.Action == VehicleAmmoLoaderSlotAction.Load)
            DoLoadAmmoSlot(ent, args.User, (used, box), ammoUid, ammo, hardpointAmmo, args.AmmoSlot);

        UpdateUi(ent.Owner, args.User);
        args.Handled = true;
    }

    private void OnUiOpened(Entity<VehicleAmmoLoaderComponent> ent, ref BoundUIOpenedEvent args)
    {
        if (!Equals(args.UiKey, VehicleAmmoLoaderUiKey.Key))
            return;

        TrackOpenLoader(ent.Owner, args.Actor);

        if (!_net.IsClient)
            UpdateUi(ent.Owner, args.Actor);
    }

    private void OnUiClosed(Entity<VehicleAmmoLoaderComponent> ent, ref BoundUIClosedEvent args)
    {
        if (!Equals(args.UiKey, VehicleAmmoLoaderUiKey.Key))
            return;

        UntrackOpenLoader(ent.Owner, args.Actor);
        ClearActiveAmmoBox(ent.Owner, args.Actor);
    }

    private void OnHandItemChanged(Entity<HandsComponent> ent, ref DidEquipHandEvent args)
    {
        UpdateOpenLoaderUis(args.User);
    }

    private void OnHandItemChanged(Entity<HandsComponent> ent, ref DidUnequipHandEvent args)
    {
        UpdateOpenLoaderUis(args.User);
    }

    private void OnBulletBoxHandSelected(Entity<BulletBoxComponent> ent, ref HandSelectedEvent args)
    {
        UpdateOpenLoaderUis(args.User);
    }

    private void OnBulletBoxHandDeselected(Entity<BulletBoxComponent> ent, ref HandDeselectedEvent args)
    {
        UpdateOpenLoaderUis(args.User);
    }

    private void OnVehicleAmmoChanged(Entity<VehicleHardpointAmmoComponent> ent, ref VehicleAmmoChangedEvent args)
    {
        if (_net.IsClient || !_topology.TryGetVehicle(ent.Owner, out var vehicle))
            return;

        UpdateOpenLoaderUisForVehicle(vehicle);
    }

    private void OnHardpointSlotsChanged(HardpointSlotsChangedEvent args)
    {
        if (_net.IsClient)
            return;

        UpdateOpenLoaderUisForVehicle(args.Vehicle);
    }

    private void OnUiSelect(Entity<VehicleAmmoLoaderComponent> ent, ref VehicleAmmoLoaderSelectMessage args)
    {
        if (!Equals(args.UiKey, VehicleAmmoLoaderUiKey.Key))
            return;

        if (args.Actor == default || !Exists(args.Actor))
            return;

        if (args.Action == VehicleAmmoLoaderSlotAction.Unload)
        {
            if (!TryGetUnloadableAmmoProvider(ent, args.Actor, args.SlotPath, out _, out var directAmmoUid, out var directAmmo, out var directHardpointAmmo, out _))
                return;

            if (GetDirectUnloadAmount(directAmmo, directHardpointAmmo, args.AmmoSlot) <= 0)
            {
                _popup.PopupClient(Loc.GetString("rmc-vehicle-ammo-loader-empty", ("box", directAmmoUid)), ent, args.Actor);
                return;
            }

            var directDoAfter = new DoAfterArgs(
                EntityManager,
                args.Actor,
                ent.Comp.LoadDelay,
                new VehicleAmmoLoaderDoAfterEvent(args.SlotPath, args.AmmoSlot, args.Action),
                ent.Owner,
                ent.Owner)
            {
                BreakOnMove = true,
                CancelDuplicate = false,
                DistanceThreshold = ent.Comp.InteractionRange,
            };

            _doAfter.TryStartDoAfter(directDoAfter);
            return;
        }

        if (!_hands.TryGetActiveItem(args.Actor, out var activeItem) ||
            activeItem is not { } activeBox ||
            !TryComp(activeBox, out BulletBoxComponent? box))
        {
            _popup.PopupClient(Loc.GetString("rmc-vehicle-ammo-loader-hold-ammo"), ent, args.Actor);
            return;
        }

        TrySetActiveAmmoBox(ent.Owner, args.Actor, activeBox);

        if (!TryGetLoadableAmmoProvider(ent, args.Actor, box, args.SlotPath, out _, out var ammoUid, out var ammo, out var hardpointAmmo))
            return;

        if (args.Action == VehicleAmmoLoaderSlotAction.Load &&
            GetLoadAmount(box, ammo, hardpointAmmo, args.AmmoSlot) <= 0)
        {
            var popup = box.Amount <= 0
                ? Loc.GetString("rmc-vehicle-ammo-loader-empty", ("box", box.Owner))
                : Loc.GetString("rmc-vehicle-ammo-loader-full", ("target", ammoUid));
            _popup.PopupClient(popup, ent, args.Actor);
            return;
        }

        var doAfter = new DoAfterArgs(
            EntityManager,
            args.Actor,
            ent.Comp.LoadDelay,
            new VehicleAmmoLoaderDoAfterEvent(args.SlotPath, args.AmmoSlot, args.Action),
            ent.Owner,
            ent.Owner,
            activeBox)
        {
            BreakOnMove = true,
            BreakOnDropItem = true,
            CancelDuplicate = false,
            DistanceThreshold = ent.Comp.InteractionRange,
            NeedHand = true,
        };

        _doAfter.TryStartDoAfter(doAfter);
    }

    private bool CanOpenUi(EntityUid loader, EntityUid user)
    {
        foreach (var actor in _ui.GetActors(loader, VehicleAmmoLoaderUiKey.Key))
        {
            if (actor == user)
                return true;

            _popup.PopupClient(Loc.GetString("rmc-vehicle-ammo-loader-in-use"), loader, user);
            return false;
        }

        return true;
    }

    private bool TrySetActiveAmmoBox(EntityUid loader, EntityUid user, EntityUid boxUid)
    {
        if (!_activeAmmoBoxes.TryGetValue(loader, out var userBoxes))
        {
            userBoxes = new Dictionary<EntityUid, EntityUid>();
            _activeAmmoBoxes[loader] = userBoxes;
        }

        userBoxes[user] = boxUid;
        return true;
    }

    private void TrySetActiveAmmoBoxFromHeld(Entity<VehicleAmmoLoaderComponent> loader, EntityUid user)
    {
        if (_hands.TryGetActiveItem(user, out var activeItem) &&
            activeItem is { } active &&
            TryComp(active, out BulletBoxComponent? _))
        {
            TrySetActiveAmmoBox(loader.Owner, user, active);
            return;
        }

        ClearActiveAmmoBox(loader.Owner, user);
    }

    private bool TryGetActiveAmmoBox(EntityUid loader, EntityUid user, out EntityUid boxUid)
    {
        boxUid = default;
        return _activeAmmoBoxes.TryGetValue(loader, out var userBoxes) &&
               userBoxes.TryGetValue(user, out boxUid);
    }

    private void ClearActiveAmmoBox(EntityUid loader, EntityUid user)
    {
        if (!_activeAmmoBoxes.TryGetValue(loader, out var userBoxes))
            return;

        userBoxes.Remove(user);
        if (userBoxes.Count == 0)
            _activeAmmoBoxes.Remove(loader);
    }

    private void TrackOpenLoader(EntityUid loader, EntityUid user)
    {
        if (!_openLoadersByUser.TryGetValue(user, out var loaders))
        {
            loaders = new HashSet<EntityUid>();
            _openLoadersByUser[user] = loaders;
        }

        loaders.Add(loader);
    }

    private void UntrackOpenLoader(EntityUid loader, EntityUid user)
    {
        if (!_openLoadersByUser.TryGetValue(user, out var loaders))
            return;

        loaders.Remove(loader);
        if (loaders.Count == 0)
            _openLoadersByUser.Remove(user);
    }

    private void UpdateOpenLoaderUis(EntityUid user)
    {
        if (_net.IsClient || !_openLoadersByUser.TryGetValue(user, out var loaders))
            return;

        var toRemove = new List<EntityUid>();
        foreach (var loader in loaders)
        {
            if (!Exists(loader))
            {
                toRemove.Add(loader);
                continue;
            }

            UpdateUi(loader, user);
        }

        foreach (var loader in toRemove)
            loaders.Remove(loader);

        if (loaders.Count == 0)
            _openLoadersByUser.Remove(user);
    }

    private void UpdateOpenLoaderUisForVehicle(EntityUid vehicle)
    {
        if (_net.IsClient || _openLoadersByUser.Count == 0)
            return;

        var staleUsers = new List<EntityUid>();
        foreach (var (user, loaders) in _openLoadersByUser)
        {
            var staleLoaders = new List<EntityUid>();
            foreach (var loader in loaders)
            {
                if (!Exists(loader))
                {
                    staleLoaders.Add(loader);
                    continue;
                }

                if (!_vehicleSystem.TryGetVehicleFromInterior(loader, out var loaderVehicle) ||
                    loaderVehicle != vehicle)
                {
                    continue;
                }

                UpdateUi(loader, user);
            }

            foreach (var loader in staleLoaders)
                loaders.Remove(loader);

            if (loaders.Count == 0)
                staleUsers.Add(user);
        }

        foreach (var user in staleUsers)
            _openLoadersByUser.Remove(user);
    }

    private bool TryGetLoadableAmmoProvider(
        Entity<VehicleAmmoLoaderComponent> loader,
        EntityUid user,
        BulletBoxComponent box,
        VehicleSlotPath? slotPath,
        out EntityUid vehicle,
        out EntityUid ammoUid,
        out BallisticAmmoProviderComponent ammo,
        out VehicleHardpointAmmoComponent hardpointAmmo)
    {
        ammoUid = default;
        ammo = default!;
        hardpointAmmo = default!;
        vehicle = default;

        if (!_vehicleSystem.TryGetVehicleFromInterior(loader.Owner, out var vehicleUid) || vehicleUid == null)
        {
            _popup.PopupClient(Loc.GetString("rmc-vehicle-ammo-loader-no-vehicle"), loader, user);
            return false;
        }

        vehicle = vehicleUid.Value;

        if (loader.Comp.BulletType != null && loader.Comp.BulletType != box.BulletType)
        {
            _popup.PopupClient(Loc.GetString("rmc-vehicle-ammo-loader-wrong-ammo"), loader, user);
            return false;
        }

        if (!TryComp(vehicle, out HardpointSlotsComponent? hardpoints) ||
            !TryComp(vehicle, out ItemSlotsComponent? itemSlots))
        {
            _popup.PopupClient(Loc.GetString("rmc-vehicle-ammo-loader-no-hardpoint"), loader, user);
            return false;
        }

        if (!TryFindAmmoProvider(vehicle, hardpoints, itemSlots, loader.Comp, box, slotPath, out var provider, out var result))
        {
            var popup = result == AmmoLoaderLookupResult.WrongAmmo
                ? Loc.GetString("rmc-vehicle-ammo-loader-wrong-ammo")
                : Loc.GetString("rmc-vehicle-ammo-loader-no-hardpoint");
            _popup.PopupClient(popup, loader, user);
            return false;
        }

        ammoUid = provider.AmmoUid;
        ammo = provider.Ammo;
        hardpointAmmo = provider.HardpointAmmo;
        return true;
    }

    private bool TryGetUnloadableAmmoProvider(
        Entity<VehicleAmmoLoaderComponent> loader,
        EntityUid user,
        VehicleSlotPath? slotPath,
        out EntityUid vehicle,
        out EntityUid ammoUid,
        out BallisticAmmoProviderComponent ammo,
        out VehicleHardpointAmmoComponent hardpointAmmo,
        out RefillableByBulletBoxComponent refill)
    {
        ammoUid = default;
        ammo = default!;
        hardpointAmmo = default!;
        refill = default!;
        vehicle = default;

        if (!_vehicleSystem.TryGetVehicleFromInterior(loader.Owner, out var vehicleUid) || vehicleUid == null)
        {
            _popup.PopupClient(Loc.GetString("rmc-vehicle-ammo-loader-no-vehicle"), loader, user);
            return false;
        }

        vehicle = vehicleUid.Value;

        if (!TryComp(vehicle, out HardpointSlotsComponent? hardpoints) ||
            !TryComp(vehicle, out ItemSlotsComponent? itemSlots))
        {
            _popup.PopupClient(Loc.GetString("rmc-vehicle-ammo-loader-no-hardpoint"), loader, user);
            return false;
        }

        if (!TryFindAmmoProvider(vehicle, hardpoints, itemSlots, loader.Comp, slotPath, out var provider))
        {
            _popup.PopupClient(Loc.GetString("rmc-vehicle-ammo-loader-no-hardpoint"), loader, user);
            return false;
        }

        ammoUid = provider.AmmoUid;
        ammo = provider.Ammo;
        hardpointAmmo = provider.HardpointAmmo;
        refill = provider.Refill;
        return true;
    }

    private bool TryFindAmmoProvider(
        EntityUid vehicle,
        HardpointSlotsComponent hardpoints,
        ItemSlotsComponent itemSlots,
        VehicleAmmoLoaderComponent loader,
        BulletBoxComponent box,
        VehicleSlotPath? slotPath,
        out VehicleMountedAmmoProvider provider,
        out AmmoLoaderLookupResult result)
    {
        provider = default;
        result = AmmoLoaderLookupResult.NotFound;

        if (slotPath is { IsValid: true } selectedPath)
        {
            if (!TryFindAmmoProvider(vehicle, hardpoints, itemSlots, loader, selectedPath, out provider))
                return false;

            if (provider.Refill.BulletType == box.BulletType)
                return true;

            result = AmmoLoaderLookupResult.WrongAmmo;
            return false;
        }

        foreach (var candidate in _topology.GetMountedAmmoProviders(vehicle, hardpoints, itemSlots))
        {
            if (!CanUseAmmoProvider(loader, candidate))
                continue;

            if (candidate.Refill.BulletType == box.BulletType)
            {
                provider = candidate;
                return true;
            }

            result = AmmoLoaderLookupResult.WrongAmmo;
        }

        return false;
    }

    private bool TryFindAmmoProvider(
        EntityUid vehicle,
        HardpointSlotsComponent hardpoints,
        ItemSlotsComponent itemSlots,
        VehicleAmmoLoaderComponent loader,
        VehicleSlotPath? slotPath,
        out VehicleMountedAmmoProvider provider)
    {
        provider = default;

        if (slotPath is { IsValid: true } selectedPath)
        {
            if (!_topology.TryGetMountedAmmoProvider(vehicle, selectedPath, out provider, hardpoints, itemSlots))
                return false;

            return CanUseAmmoProvider(loader, provider);
        }

        foreach (var candidate in _topology.GetMountedAmmoProviders(vehicle, hardpoints, itemSlots))
        {
            if (!CanUseAmmoProvider(loader, candidate))
                continue;

            provider = candidate;
            return true;
        }

        return false;
    }

    private static bool CanUseAmmoProvider(VehicleAmmoLoaderComponent loader, VehicleMountedAmmoProvider provider)
    {
        return string.IsNullOrWhiteSpace(loader.HardpointType) ||
               string.Equals(provider.Slot.HardpointType, loader.HardpointType, StringComparison.OrdinalIgnoreCase);
    }

    private void UpdateUi(EntityUid loader, EntityUid user)
    {
        if (_net.IsClient)
            return;

        if (!TryComp(loader, out VehicleAmmoLoaderComponent? loaderComp))
            return;

        if (!_vehicleSystem.TryGetVehicleFromInterior(loader, out var vehicleUid) || vehicleUid == null)
            return;

        if (!TryComp(vehicleUid.Value, out HardpointSlotsComponent? hardpoints) ||
            !TryComp(vehicleUid.Value, out ItemSlotsComponent? itemSlots))
        {
            return;
        }

        TrySetActiveAmmoBoxFromHeld((loader, loaderComp), user);

        BulletBoxComponent? heldBox = null;
        if (TryGetActiveAmmoBox(loader, user, out var boxUid))
            TryComp(boxUid, out heldBox);

        var providers = _topology.GetMountedAmmoProviders(vehicleUid.Value, hardpoints, itemSlots);
        var entries = new List<VehicleAmmoLoaderUiEntry>(providers.Count);

        foreach (var provider in providers)
        {
            if (!CanUseAmmoProvider(loaderComp, provider))
                continue;

            if (loaderComp.BulletType != null && loaderComp.BulletType != provider.Refill.BulletType)
                continue;

            var magazineSize = _hardpointAmmo.GetMagazineSize(provider.HardpointAmmo, provider.Ammo);
            var ammoSlots = GetAmmoSlotUiEntries(heldBox, provider.Refill.BulletType, provider.Ammo, provider.HardpointAmmo, magazineSize);
            var canLoad = false;
            var canUnload = false;
            foreach (var ammoSlot in ammoSlots)
            {
                canLoad |= ammoSlot.CanLoad;
                canUnload |= ammoSlot.CanUnload;
            }

            entries.Add(new VehicleAmmoLoaderUiEntry(
                provider.Slot.Path,
                provider.Slot.HardpointType,
                Name(provider.AmmoUid),
                GetNetEntity(provider.AmmoUid),
                provider.Refill.BulletType,
                magazineSize,
                ammoSlots,
                canLoad,
                canUnload));
        }

        _ui.SetUiState(
            loader,
            VehicleAmmoLoaderUiKey.Key,
            new VehicleAmmoLoaderUiState(
                entries,
                heldBox?.Amount ?? 0,
                heldBox?.Max ?? 0,
                heldBox?.BulletType));
    }

    private List<VehicleAmmoLoaderUiAmmoSlot> GetAmmoSlotUiEntries(
        BulletBoxComponent? heldBox,
        EntProtoId? ammoPrototype,
        BallisticAmmoProviderComponent ammo,
        VehicleHardpointAmmoComponent hardpointAmmo,
        int magazineSize)
    {
        var entries = new List<VehicleAmmoLoaderUiAmmoSlot>(Math.Max(1, hardpointAmmo.MaxStoredMagazines));
        foreach (var slot in _hardpointAmmo.GetAmmoQueueSlots(hardpointAmmo, ammo))
        {
            entries.Add(new VehicleAmmoLoaderUiAmmoSlot(
                slot.SlotIndex,
                slot.IsReadySlot
                    ? Loc.GetString("rmc-vehicle-ammo-loader-ui-ready-slot")
                    : (slot.SlotIndex + 1).ToString(),
                slot.Rounds,
                slot.Capacity,
                CanLoadSlot(heldBox, ammoPrototype, ammo, hardpointAmmo, slot.SlotIndex),
                _hardpointAmmo.HasUnloadRounds(hardpointAmmo, ammo, slot.SlotIndex),
                slot.IsReadySlot));
        }

        return entries;
    }

    private bool CanLoadSlot(
        BulletBoxComponent? heldBox,
        EntProtoId? ammoPrototype,
        BallisticAmmoProviderComponent ammo,
        VehicleHardpointAmmoComponent hardpointAmmo,
        int ammoSlot)
    {
        return heldBox != null &&
               heldBox.Amount > 0 &&
               ammoPrototype != null &&
               heldBox.BulletType == ammoPrototype &&
               _hardpointAmmo.HasLoadSpace(hardpointAmmo, ammo, ammoSlot);
    }

    private int GetLoadAmount(
        BulletBoxComponent box,
        BallisticAmmoProviderComponent ammo,
        VehicleHardpointAmmoComponent hardpointAmmo,
        int ammoSlot)
    {
        return _hardpointAmmo.GetLoadAmount(hardpointAmmo, ammo, ammoSlot, box.Amount);
    }

    private int GetDirectUnloadAmount(
        BallisticAmmoProviderComponent ammo,
        VehicleHardpointAmmoComponent hardpointAmmo,
        int ammoSlot)
    {
        return _hardpointAmmo.GetUnloadAmount(hardpointAmmo, ammo, ammoSlot);
    }

    private void DoLoadAmmoSlot(
        Entity<VehicleAmmoLoaderComponent> loader,
        EntityUid user,
        Entity<BulletBoxComponent> box,
        EntityUid ammoUid,
        BallisticAmmoProviderComponent ammo,
        VehicleHardpointAmmoComponent hardpointAmmo,
        int ammoSlot)
    {
        var transferAmount = GetLoadAmount(box.Comp, ammo, hardpointAmmo, ammoSlot);
        if (transferAmount <= 0)
            return;

        if (!_bulletBox.TryConsume(box, transferAmount))
            return;

        if (box.Comp.Amount <= 0)
            Del(box.Owner);

        _hardpointAmmo.TryLoadIntoSlot((ammoUid, hardpointAmmo), ammo, ammoSlot, transferAmount);

        _popup.PopupClient(Loc.GetString("rmc-vehicle-ammo-loader-loaded", ("amount", transferAmount), ("target", ammoUid)), loader, user);
    }

    private void DoDirectUnloadAmmoSlot(
        Entity<VehicleAmmoLoaderComponent> loader,
        EntityUid user,
        EntityUid ammoUid,
        BallisticAmmoProviderComponent ammo,
        VehicleHardpointAmmoComponent hardpointAmmo,
        RefillableByBulletBoxComponent refill,
        int ammoSlot)
    {
        var transferAmount = GetDirectUnloadAmount(ammo, hardpointAmmo, ammoSlot);
        if (transferAmount <= 0)
            return;

        var unloadedAmount = SpawnUnloadedAmmo(user, refill, transferAmount);
        if (unloadedAmount <= 0)
            return;

        _hardpointAmmo.TryUnloadFromSlot((ammoUid, hardpointAmmo), ammo, ammoSlot, unloadedAmount);

        _popup.PopupClient(Loc.GetString("rmc-vehicle-ammo-loader-unloaded", ("amount", unloadedAmount), ("target", ammoUid)), loader, user);
    }

    private int SpawnUnloadedAmmo(EntityUid user, RefillableByBulletBoxComponent refill, int amount)
    {
        if (amount <= 0 || refill.BulletType is not { } prototype)
            return 0;

        var remaining = amount;
        var unloaded = 0;
        while (remaining > 0)
        {
            var spawned = Spawn(prototype, Transform(user).Coordinates);
            if (!TryComp(spawned, out BulletBoxComponent? box))
            {
                _hands.PickupOrDrop(user, spawned);
                return unloaded + 1;
            }

            var boxMax = Math.Max(1, box.Max);
            var boxAmount = Math.Min(remaining, boxMax);
            if (!_bulletBox.TrySetAmount((spawned, box), boxAmount))
            {
                Del(spawned);
                return unloaded;
            }

            _hands.PickupOrDrop(user, spawned);
            remaining -= boxAmount;
            unloaded += boxAmount;
        }

        return unloaded;
    }

}

internal enum AmmoLoaderLookupResult : byte
{
    NotFound,
    WrongAmmo,
}

[Serializable, NetSerializable]
public sealed partial class VehicleAmmoLoaderDoAfterEvent : DoAfterEvent
{
    [DataField(required: true)]
    public VehicleSlotPath SlotPath;

    [DataField]
    public int AmmoSlot;

    [DataField]
    public VehicleAmmoLoaderSlotAction Action;

    public VehicleAmmoLoaderDoAfterEvent()
    {
    }

    public VehicleAmmoLoaderDoAfterEvent(VehicleSlotPath slotPath, int ammoSlot, VehicleAmmoLoaderSlotAction action)
    {
        SlotPath = slotPath;
        AmmoSlot = ammoSlot;
        Action = action;
    }

    public override DoAfterEvent Clone()
    {
        return new VehicleAmmoLoaderDoAfterEvent(SlotPath, AmmoSlot, Action);
    }

    public override bool IsDuplicate(DoAfterEvent other)
    {
        return other is VehicleAmmoLoaderDoAfterEvent loaderEvent
               && loaderEvent.SlotPath == SlotPath
               && loaderEvent.AmmoSlot == AmmoSlot
               && loaderEvent.Action == Action;
    }
}
