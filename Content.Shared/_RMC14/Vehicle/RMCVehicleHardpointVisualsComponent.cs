using System;
using System.Collections.Generic;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Vehicle;

[RegisterComponent, NetworkedComponent]
public sealed partial class RMCVehicleHardpointVisualsComponent : Component
{
    [DataField]
    public List<RMCVehicleHardpointLayerState> Layers = new();
}

[Serializable, NetSerializable, DataDefinition]
public partial record struct RMCVehicleHardpointLayerState
{
    [DataField(required: true)]
    public string Layer { get; set; } = string.Empty;

    [DataField]
    public string State { get; set; } = string.Empty;

    public RMCVehicleHardpointLayerState(string layer, string state)
    {
        Layer = layer;
        State = state;
    }
}

[Serializable, NetSerializable]
public sealed class RMCVehicleHardpointVisualsComponentState : ComponentState
{
    public readonly List<RMCVehicleHardpointLayerState> Layers;

    public RMCVehicleHardpointVisualsComponentState(List<RMCVehicleHardpointLayerState> layers)
    {
        Layers = layers;
    }
}
