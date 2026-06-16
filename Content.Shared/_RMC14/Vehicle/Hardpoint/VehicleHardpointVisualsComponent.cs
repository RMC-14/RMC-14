using System;
using System.Collections.Generic;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Vehicle;

[RegisterComponent, NetworkedComponent]
public sealed partial class VehicleHardpointVisualsComponent : Component
{
    public List<VehicleHardpointLayerState> Layers = new();
}

[Serializable, NetSerializable, DataDefinition]
public partial record struct VehicleHardpointLayerState
{
    [DataField(required: true)]
    public string Layer { get; set; } = string.Empty;

    [DataField]
    public string State { get; set; } = string.Empty;

    public VehicleHardpointLayerState(string layer, string state)
    {
        Layer = layer;
        State = state;
    }
}

[Serializable, NetSerializable]
public enum VehicleHardpointVisualsVisuals : byte
{
    Layers,
}
