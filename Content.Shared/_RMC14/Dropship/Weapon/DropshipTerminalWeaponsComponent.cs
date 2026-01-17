using Content.Shared._RMC14.Dropship.AttachmentPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Dropship.Weapon;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(SharedDropshipWeaponSystem))]
public sealed partial class DropshipTerminalWeaponsComponent : Component
{
    [DataField, AutoNetworkedField]
    public Screen ScreenOne;

    [DataField, AutoNetworkedField]
    public Screen ScreenTwo;

    [DataField, AutoNetworkedField]
    public EntityUid? Target;

    [DataField, AutoNetworkedField]
    public Vector2i Offset;

    [DataField, AutoNetworkedField]
    public Vector2i OffsetLimit = new(12, 12);

    [DataField, AutoNetworkedField]
    public List<TargetEnt> Targets = new();

    [DataField, AutoNetworkedField]
    public int TargetsPage;

    [DataField, AutoNetworkedField]
    public List<TargetEnt> Medevacs = new();

    [DataField, AutoNetworkedField]
    public int MedevacsPage;

    [DataField, AutoNetworkedField]
    public List<TargetEnt> Fultons = new();

    [DataField, AutoNetworkedField]
    public int FultonsPage;

    [DataField, AutoNetworkedField]
    public bool NightVision;

    [DataField, AutoNetworkedField]
    public NetEntity? SelectedSystem;

    /// <summary>
    ///     The entity being looked at by the camera view.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? CameraTarget;

    /// <summary>
    ///     A list of all fire missions currently stored on this component.
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<FireMissionData> FireMissions = [];

    /// <summary>
    ///     The ID of the fire mission currently being viewed or edited on screen one.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int? ScreenOneViewingFireMissionId;

    /// <summary>
    ///     The ID of the fire mission currently being viewed or edited on screen two.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int? ScreenTwoViewingFireMissionId;

    /// <summary>
    ///     The lowest timing at which a weapon offset can be set.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int MinTiming = 1;

    /// <summary>
    ///     The maximum length of the fire mission.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int MaxTiming = 12;

    /// <summary>
    ///     The offsets allowed to be used for a weapon during a fire mission based on the <see cref="DropshipWeaponPointLocation"/>
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<DropshipWeaponPointLocation, List<int?>> AllowedOffsets = new()
    {
        { DropshipWeaponPointLocation.PortFore, new List<int?> { -6, -5, -4, -3, -2, -1, 0, null } },
        { DropshipWeaponPointLocation.PortWing, new List<int?> { -6, -5, -4, -3, -2, -1, 0, null } },
        { DropshipWeaponPointLocation.StarboardFore, new List<int?> { null, 0, 1, 2, 3, 4, 5, 6 } },
        { DropshipWeaponPointLocation.StarboardWing, new List<int?> { null, 0, 1, 2, 3, 4, 5, 6 } },
    };

    /// <summary>
    ///     The direction in which the fire mission will advance.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Direction StrikeVector = Direction.North;

    [DataField, AutoNetworkedField]
    public int ScreenOneFireMissionPage;

    [DataField, AutoNetworkedField]
    public int ScreenTwoFireMissionPage;

    /// <summary>
    ///     The max length of a fire mission's name. Values higher than 40 might mess up how some of the UI looks.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int MaxFireMissionNameLength = 40;

    [DataRecord]
    [Serializable, NetSerializable]
    public record struct Screen(
        DropshipTerminalWeaponsScreen State,
        NetEntity? Weapon,
        int? FireMissionId
    );

    [DataRecord]
    [Serializable, NetSerializable]
    public readonly record struct TargetEnt(
        NetEntity Id,
        string Name
    );
}

[Serializable, NetSerializable]
public readonly record struct WeaponOffsetData(
    NetEntity WeaponId,
    int Step,
    int? Offset
);

[Serializable, NetSerializable]
public readonly record struct FireMissionData(
    int Id,
    string Name,
    List<WeaponOffsetData> WeaponOffsets
);

public sealed class WeaponDisplayInfo
{
    public string Name { get; set; } = "";
    public int Ammo { get; set; }
    public int MaxAmmo { get; set; }
    public int AmmoConsumption { get; set; }
    public int FireDelay { get; set; }
    public List<WeaponOffsetData> Offsets { get; set; } = [];
}

[Serializable, NetSerializable]
public enum DropshipWeaponStrikeType
{
    Direct,
    FireMission,
}
