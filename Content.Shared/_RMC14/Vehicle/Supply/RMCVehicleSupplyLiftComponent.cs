using System;
using System.Collections.Generic;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.Vehicle.Supply;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true), AutoGenerateComponentPause]
public sealed partial class RMCVehicleSupplyLiftComponent : Component
{
    [DataField]
    public float Radius = 2f;

    [DataField, AutoNetworkedField]
    public RMCVehicleSupplyLiftMode Mode;

    [DataField]
    public RMCVehicleSupplyLiftMode? NextMode;

    [DataField, AutoNetworkedField]
    public bool Busy;

    [DataField, AutoNetworkedField]
    public string LoweredState = "supply_elevator_lowered";

    [DataField, AutoNetworkedField]
    public string LoweringState = "supply_elevator_lowering";

    [DataField, AutoNetworkedField]
    public string RaisedState = "supply_elevator_raised";

    [DataField, AutoNetworkedField]
    public string RaisingState = "supply_elevator_raising";

    public EntityUid? Audio;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan? ToggledAt;

    [DataField]
    public TimeSpan ToggleDelay = TimeSpan.FromSeconds(17);

    [DataField]
    public TimeSpan RaiseSoundDelay = TimeSpan.FromSeconds(5);

    [DataField]
    public TimeSpan RaiseDelay = TimeSpan.FromSeconds(12.5);

    [DataField]
    public TimeSpan LowerDelay = TimeSpan.FromSeconds(2);

    [DataField]
    public TimeSpan LowerSoundDelay = TimeSpan.FromSeconds(2);

    [DataField]
    public SoundSpecifier? LoweringSound = new SoundPathSpecifier("/Audio/_RMC14/Machines/asrs_lowering.ogg");

    [DataField]
    public SoundSpecifier? RaisingSound = new SoundPathSpecifier("/Audio/_RMC14/Machines/asrs_raising.ogg");

    public object? LoweringAnimation;

    public object? RaisingAnimation;

    [NonSerialized]
    public string PendingVehicle = string.Empty;

    [NonSerialized]
    public EntityUid? ActiveVehicle;

    [NonSerialized]
    public string ActiveVehicleId = string.Empty;

    [NonSerialized]
    public readonly HashSet<string> Deployed = new();

    [NonSerialized]
    public readonly Dictionary<string, int> Stored = new();
}

[Serializable, NetSerializable]
public enum RMCVehicleSupplyLiftLayers
{
    Base,
}

[Serializable, NetSerializable]
public enum RMCVehicleSupplyLiftMode
{
    Lowered,
    Raised,
    Lowering,
    Raising,
    Preparing,
}
