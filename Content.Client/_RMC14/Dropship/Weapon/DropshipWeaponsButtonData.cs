using Robust.Client.UserInterface.Controls;

namespace Content.Client._RMC14.Dropship.Weapon;

public readonly record struct DropshipWeaponsButtonData(
    LocId Text,
    Action<BaseButton.ButtonEventArgs> OnPressed,
    NetEntity? Weapon = null
);
