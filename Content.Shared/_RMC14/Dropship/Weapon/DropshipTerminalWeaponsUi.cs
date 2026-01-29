using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Dropship.Weapon;

[Serializable, NetSerializable]
public enum DropshipTerminalWeaponsUi
{
    Key,
}

[Serializable, NetSerializable]
public enum DropshipTerminalWeaponsScreen
{
    Main = 0,
    Equip,
    Target,
    Strike,
    StrikeWeapon,
    Cams,
    SelectingWeapon,
    Medevac,
    Fulton,
    Paradrop,
    Spotlight,
    TacMap,
    FireMissionCreate,
    FireMissionView,
    FireMissionEdit,
    StrikeFireMission,
    StrikeVector,
}

[Serializable, NetSerializable]
public sealed class DropshipTerminalWeaponsChangeScreenMsg(bool first, DropshipTerminalWeaponsScreen screen) : BoundUserInterfaceMessage
{
    public readonly bool First = first;
    public readonly DropshipTerminalWeaponsScreen Screen = screen;
}

[Serializable, NetSerializable]
public sealed class DropshipTerminalWeaponsChooseWeaponMsg(bool first, NetEntity weapon) : BoundUserInterfaceMessage
{
    public readonly bool First = first;
    public readonly NetEntity Weapon = weapon;
}

[Serializable, NetSerializable]
public sealed class DropshipTerminalWeaponsChooseMedevacMsg(bool first) : BoundUserInterfaceMessage
{
    public readonly bool First = first;
}

[Serializable, NetSerializable]
public sealed class DropshipTerminalWeaponsChooseFultonMsg(bool first) : BoundUserInterfaceMessage
{
    public readonly bool First = first;
}

[Serializable, NetSerializable]
public sealed class DropshipTerminalWeaponsChooseParaDropMsg(bool first) : BoundUserInterfaceMessage
{
    public readonly bool First = first;
}

[Serializable, NetSerializable]
public sealed class DropshipTerminalWeaponsChooseSpotlightMsg(bool first, NetEntity slot) : BoundUserInterfaceMessage
{
    public readonly bool First = first;
    public readonly NetEntity Slot = slot;
}

[Serializable, NetSerializable]
public sealed class DropshipTerminalWeaponsExitMsg(bool first) : BoundUserInterfaceMessage
{
    public readonly bool First = first;
}

[Serializable, NetSerializable]
public sealed class DropshipTerminalWeaponsCancelMsg(bool first) : BoundUserInterfaceMessage
{
    public readonly bool First = first;
}

[Serializable, NetSerializable]
public sealed class DropshipTerminalWeaponsAdjustOffsetMsg(Direction direction) : BoundUserInterfaceMessage
{
    public readonly Direction Direction = direction;
}

[Serializable, NetSerializable]
public sealed class DropshipTerminalWeaponsResetOffsetMsg : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class DropshipTerminalWeaponsFireMsg(bool first) : BoundUserInterfaceMessage
{
    public readonly bool First = first;
}

[Serializable, NetSerializable]
public sealed class DropshipTerminalWeaponsNightVisionMsg(bool on) : BoundUserInterfaceMessage
{
    public readonly bool On = on;
}

[Serializable, NetSerializable]
public sealed class DropshipTerminalWeaponsTargetsPreviousMsg : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class DropshipTerminalWeaponsTargetsNextMsg : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class DropshipTerminalWeaponsTargetsSelectMsg(NetEntity target) : BoundUserInterfaceMessage
{
    public readonly NetEntity Target = target;
}

[Serializable, NetSerializable]
public sealed class DropshipTerminalWeaponsMedevacPreviousMsg : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class DropshipTerminalWeaponsMedevacNextMsg : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class DropshipTerminalWeaponsMedevacSelectMsg(NetEntity target) : BoundUserInterfaceMessage
{
    public readonly NetEntity Target = target;
}

[Serializable, NetSerializable]
public sealed class DropshipTerminalWeaponsFultonPreviousMsg : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class DropshipTerminalWeaponsFultonNextMsg : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class DropshipTerminalWeaponsFultonSelectMsg(NetEntity target) : BoundUserInterfaceMessage
{
    public readonly NetEntity Target = target;
}

[Serializable, NetSerializable]
public sealed class DropShipTerminalWeaponsParaDropTargetSelectMsg(bool on) : BoundUserInterfaceMessage
{
    public readonly bool On = on;
}

[Serializable, NetSerializable]
public sealed class DropShipTerminalWeaponsSpotlightToggleMsg(bool on) : BoundUserInterfaceMessage
{
    public readonly bool On = on;
}

[Serializable, NetSerializable]
public sealed class DropshipTerminalWeaponsCreateFireMissionMsg(string name) : BoundUserInterfaceMessage
{
    public readonly string Name = name;
}

[Serializable, NetSerializable]
public sealed class DropshipTerminalWeaponsViewFireMissionMsg(bool first, int id) : BoundUserInterfaceMessage
{
    public readonly bool First = first;
    public readonly int Id = id;
}

[Serializable, NetSerializable]
public sealed class DropshipTerminalWeaponsEditFireMissionMsg(bool first, int missionId, int step, int? offset) : BoundUserInterfaceMessage
{
    public readonly bool First = first;
    public readonly int MissionId = missionId;
    public readonly int Step = step;
    public readonly int? Offset = offset;
}

[Serializable, NetSerializable]
public sealed class DropshipTerminalWeaponsDeleteFireMissionMsg(bool first) : BoundUserInterfaceMessage
{
    public readonly bool First = first;
}

[Serializable, NetSerializable]
public sealed class DropshipTerminalWeaponsSelectFireMissionMsg(bool first, int id) : BoundUserInterfaceMessage
{
    public readonly bool First = first;
    public readonly int Id = id;
}

[Serializable, NetSerializable]
public sealed class DropshipTerminalWeaponsSelectStrikeVectorMsg(bool first, Direction direction) : BoundUserInterfaceMessage
{
    public readonly bool First = first;
    public readonly Direction Direction = direction;
}

[Serializable, NetSerializable]
public sealed class DropshipTerminalWeaponsFireMissionPreviousMsg(bool first, int targetsPerPage, int fixedButtonsCount) : BoundUserInterfaceMessage
{
    public readonly bool First = first;
    public readonly int TargetsPerPage = targetsPerPage;
    public readonly int FixedButtonsCount = fixedButtonsCount;
}

[Serializable, NetSerializable]
public sealed class DropshipTerminalWeaponsFireMissionNextMsg(bool first, int targetsPerPage, int fixedButtonsCount) : BoundUserInterfaceMessage
{
    public readonly bool First = first;
    public readonly int TargetsPerPage = targetsPerPage;
    public readonly int FixedButtonsCount = fixedButtonsCount;
}
