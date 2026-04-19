using Content.Shared.Containers.ItemSlots;
using Content.Shared.Input;
using Content.Shared.Vehicle.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Vehicle;

public sealed class VehicleSpotlightSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly VehicleSystem _rmcVehicles = default!;
    [Dependency] private readonly SharedPointLightSystem _lights = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<VehicleSpotlightComponent, ComponentStartup>(OnSpotlightStartup);
        SubscribeLocalEvent<HardpointSlotsChangedEvent>(OnHardpointSlotsChanged);

        if (_net.IsClient)
        {
            CommandBinds.Builder
                .Bind(ContentKeyFunctions.FlipObject, InputCmdHandler.FromDelegate(session =>
                {
                    if (session?.AttachedEntity is not { } user)
                        return;

                    EntityUid? vehicleUid = null;
                    if (TryComp<VehicleOperatorComponent>(user, out var op) && op.Vehicle != null)
                    {
                        vehicleUid = op.Vehicle.Value;
                    }
                    else if (_rmcVehicles.TryGetVehicleFromInterior(user, out var interiorVehicle) && interiorVehicle != null)
                    {
                        vehicleUid = interiorVehicle.Value;
                    }

                    if (vehicleUid == null)
                        return;

                    RaiseNetworkEvent(new VehicleSpotlightToggleRequestEvent(GetNetEntity(vehicleUid.Value)));
                }, handle: true))
                .Register<VehicleSpotlightSystem>();
        }

        SubscribeNetworkEvent<VehicleSpotlightToggleRequestEvent>(OnSpotlightToggleRequest);
    }

    private void OnSpotlightStartup(Entity<VehicleSpotlightComponent> ent, ref ComponentStartup args)
    {
        EnsureBase(ent.Comp);
        if (_net.IsServer)
            RecalculateFromHardpoints(ent.Owner, ent.Comp);

        ApplySpotlight(ent.Owner, ent.Comp);
    }

    private void OnHardpointSlotsChanged(HardpointSlotsChangedEvent args)
    {
        if (!_net.IsServer)
            return;

        if (!TryComp(args.Vehicle, out VehicleSpotlightComponent? spotlight))
            return;

        RecalculateFromHardpoints(args.Vehicle, spotlight);
        ApplySpotlight(args.Vehicle, spotlight);
        Dirty(args.Vehicle, spotlight);
    }

    private void OnSpotlightToggleRequest(VehicleSpotlightToggleRequestEvent ev, EntitySessionEventArgs args)
    {
        if (!_net.IsServer)
            return;

        if (args.SenderSession.AttachedEntity is not { } user)
            return;

        var vehicle = GetEntity(ev.Vehicle);
        if (!TryComp(vehicle, out VehicleComponent? vehicleComp) || vehicleComp.Operator != user)
            return;

        if (!TryComp(vehicle, out VehicleSpotlightComponent? spotlight))
            return;

        spotlight.Enabled = !spotlight.Enabled;
        ApplySpotlight(vehicle, spotlight);
        Dirty(vehicle, spotlight);
    }

    private void ApplySpotlight(EntityUid uid, VehicleSpotlightComponent spotlight)
    {
        SharedPointLightComponent? light = null;
        if (!_lights.ResolveLight(uid, ref light))
            return;

        _lights.SetRadius(uid, spotlight.Radius, light);
        _lights.SetEnergy(uid, spotlight.Energy, light);
        _lights.SetSoftness(uid, spotlight.Softness, light);
        _lights.SetEnabled(uid, spotlight.Enabled, light);
    }

    private static void EnsureBase(VehicleSpotlightComponent spotlight)
    {
        if (spotlight.BaseInitialized)
            return;

        spotlight.BaseInitialized = true;
        spotlight.BaseRadius = spotlight.Radius;
        spotlight.BaseEnergy = spotlight.Energy;
        spotlight.BaseSoftness = spotlight.Softness;
    }

    private void RecalculateFromHardpoints(
        EntityUid vehicle,
        VehicleSpotlightComponent spotlight,
        HardpointSlotsComponent? hardpoints = null,
        ItemSlotsComponent? itemSlots = null)
    {
        EnsureBase(spotlight);

        var radius = spotlight.BaseRadius;
        var energy = spotlight.BaseEnergy;
        var softness = spotlight.BaseSoftness;

        if (Resolve(vehicle, ref hardpoints, ref itemSlots, logMissing: false))
        {
            foreach (var slot in hardpoints.Slots)
            {
                if (string.IsNullOrWhiteSpace(slot.Id))
                    continue;

                if (!_itemSlots.TryGetSlot(vehicle, slot.Id, out var itemSlot, itemSlots) || !itemSlot.HasItem)
                    continue;

                var item = itemSlot.Item!.Value;
                if (!TryComp(item, out VehicleSpotlightModifierComponent? modifier))
                    continue;

                radius = radius * modifier.RadiusMultiplier + modifier.RadiusAdd;
                energy = energy * modifier.EnergyMultiplier + modifier.EnergyAdd;
                softness = softness * modifier.SoftnessMultiplier + modifier.SoftnessAdd;
            }
        }

        spotlight.Radius = radius;
        spotlight.Energy = energy;
        spotlight.Softness = softness;
    }
}

[Serializable, NetSerializable]
public sealed partial class VehicleSpotlightToggleRequestEvent : EntityEventArgs
{
    public NetEntity Vehicle;

    public VehicleSpotlightToggleRequestEvent()
    {
    }

    public VehicleSpotlightToggleRequestEvent(NetEntity vehicle)
    {
        Vehicle = vehicle;
    }
}
