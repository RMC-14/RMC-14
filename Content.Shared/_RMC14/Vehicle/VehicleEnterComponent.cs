using System.Numerics;
using Content.Shared._RMC14.Marines.Skills;
using Content.Shared.DoAfter;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.Vehicle;

[DataDefinition]
public sealed partial class VehicleEntryPoint
{
    [DataField(required: true)]
    public Vector2 Offset;

    [DataField]
    public float Radius = 0.6f;

    [DataField]
    public Vector2? InteriorCoords;
}

[RegisterComponent, NetworkedComponent]
[Access(typeof(RMCVehicleSystem))]
public sealed partial class VehicleEnterComponent : Component
{
    [DataField(required: true)]
    public ResPath InteriorPath;

    [DataField]
    public List<VehicleEntryPoint> EntryPoints = new();

    [DataField]
    public float EnterDoAfter = 0f;

    [DataField]
    public float ExitDoAfter = 0f;

    [DataField]
    public Vector2 ExitOffset = Vector2.Zero;
}

[RegisterComponent, NetworkedComponent]
[Access(typeof(RMCVehicleSystem))]
public sealed partial class VehicleExitComponent : Component
{
    [DataField]
    public int EntryIndex;
}

[Serializable, NetSerializable]
public sealed partial class VehicleEnterDoAfterEvent : SimpleDoAfterEvent
{
    [DataField(required: true)]
    public int EntryIndex;

    public override DoAfterEvent Clone()
    {
        return new VehicleEnterDoAfterEvent
        {
            EntryIndex = EntryIndex,
        };
    }
}

[Serializable, NetSerializable]
public sealed partial class VehicleExitDoAfterEvent : SimpleDoAfterEvent;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RMCVehicleSystem))]
public sealed partial class VehicleDriverSeatComponent : Component
{
    [DataField, AutoNetworkedField]
    public SkillWhitelist Skills = new();
}
