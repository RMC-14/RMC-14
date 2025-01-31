﻿using Robust.Shared.Serialization;

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
