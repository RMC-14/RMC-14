using System;
using System.Collections.Generic;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Vehicle;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Vehicle;

[RegisterComponent, NetworkedComponent]
[Access(typeof(RMCVehicleWheelSystem))]
public sealed partial class RMCVehicleWheelItemComponent : Component;

[RegisterComponent, NetworkedComponent]
[Access(typeof(RMCVehicleWheelSystem))]
public sealed partial class RMCVehicleWheelSlotsComponent : Component
{
    public const string WheelComponentId = "RMCVehicleWheelItem";

    public const string HardpointTypeId = "Wheel";

    [DataField]
    public int SlotCount = 1;

    [DataField]
    public List<string> Slots = new();

    [DataField]
    public string SlotPrefix = "wheel";

    [DataField]
    public EntityWhitelist WheelWhitelist = new()
    {
        Components = new[] { WheelComponentId },
    };
}

[Serializable, NetSerializable]
public enum RMCVehicleWheelVisuals : byte
{
    HasAllWheels,
    WheelCount,
}

public static class RMCVehicleWheelLayers
{
    public const string Wheels = "rmc-wheels";
}
