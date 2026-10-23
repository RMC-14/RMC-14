using System;
using Content.Shared._RMC14.Marines.Skills;
using Content.Shared.DoAfter;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Vehicle;

[RegisterComponent, NetworkedComponent]
[Access(typeof(VehicleSystem))]
public sealed partial class VehicleDemolitionComponent : Component
{
    [DataField]
    public EntityWhitelist Whitelist = new()
    {
        Components = new[] { "RMCVehicleDemolitionCharge" },
    };

    [DataField]
    public SkillWhitelist Skills = new()
    {
        All = { ["RMCSkillEngineer"] = 1 },
    };

    [DataField]
    public float DoAfter = 45f;

    [DataField]
    public float TimerDelay = 10f;

    [DataField]
    public float BeepInterval = 1f;

    [DataField]
    public SoundSpecifier? BeepSound = new SoundPathSpecifier("/Audio/Effects/beep1.ogg", AudioParams.Default.WithVolume(4));

    [DataField]
    public EntProtoId Explosion = "RMCVehicleWreckExplosion";

    [DataField]
    public bool DeleteUsedItem = true;

    [DataField]
    public SoundSpecifier? PlantSound = new SoundPathSpecifier("/Audio/Effects/beep1.ogg");

    [ViewVariables]
    public bool Rigging;

    [ViewVariables]
    public bool Armed;

    [ViewVariables]
    public TimeSpan DetonateAt;

    [ViewVariables]
    public TimeSpan NextBeepAt;
}

[Serializable, NetSerializable]
public sealed partial class VehicleDemolitionDoAfterEvent : SimpleDoAfterEvent;
