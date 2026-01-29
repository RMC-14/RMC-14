using Content.Shared.Input;
using Content.Shared.Vehicle.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Vehicle;

public sealed class RMCVehicleSpotlightSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly RMCVehicleSystem _rmcVehicles = default!;
    [Dependency] private readonly SharedPointLightSystem _lights = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCVehicleSpotlightComponent, ComponentStartup>(OnSpotlightStartup);

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

                    RaiseNetworkEvent(new RMCVehicleSpotlightToggleRequestEvent(GetNetEntity(vehicleUid.Value)));
                }, handle: true))
                .Register<RMCVehicleSpotlightSystem>();
        }

        SubscribeNetworkEvent<RMCVehicleSpotlightToggleRequestEvent>(OnSpotlightToggleRequest);
    }

    private void OnSpotlightStartup(Entity<RMCVehicleSpotlightComponent> ent, ref ComponentStartup args)
    {
        ApplySpotlight(ent.Owner, ent.Comp);
    }

    private void OnSpotlightToggleRequest(RMCVehicleSpotlightToggleRequestEvent ev, EntitySessionEventArgs args)
    {
        if (!_net.IsServer)
            return;

        if (args.SenderSession.AttachedEntity is not { } user)
            return;

        var vehicle = GetEntity(ev.Vehicle);
        if (!TryComp(vehicle, out VehicleComponent? vehicleComp) || vehicleComp.Operator != user)
            return;

        if (!TryComp(vehicle, out RMCVehicleSpotlightComponent? spotlight))
            return;

        spotlight.Enabled = !spotlight.Enabled;
        ApplySpotlight(vehicle, spotlight);
        Dirty(vehicle, spotlight);
    }

    private void ApplySpotlight(EntityUid uid, RMCVehicleSpotlightComponent spotlight)
    {
        SharedPointLightComponent? light = null;
        if (!_lights.ResolveLight(uid, ref light))
            return;

        _lights.SetRadius(uid, spotlight.Radius, light);
        _lights.SetEnergy(uid, spotlight.Energy, light);
        _lights.SetSoftness(uid, spotlight.Softness, light);
        _lights.SetEnabled(uid, spotlight.Enabled, light);
    }
}

[Serializable, NetSerializable]
public sealed partial class RMCVehicleSpotlightToggleRequestEvent : EntityEventArgs
{
    public NetEntity Vehicle;

    public RMCVehicleSpotlightToggleRequestEvent()
    {
    }

    public RMCVehicleSpotlightToggleRequestEvent(NetEntity vehicle)
    {
        Vehicle = vehicle;
    }
}
