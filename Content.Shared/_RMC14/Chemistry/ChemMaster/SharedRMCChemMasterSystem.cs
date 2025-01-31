using Robust.Shared.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Shared._RMC14.Chemistry.ChemMaster;

public abstract partial class SharedRMCChemMasterSystem : EntitySystem
{

}

[Serializable, NetSerializable]
public sealed partial class OpenChangePillBottleColorMenuMessage : BoundUserInterfaceMessage
{
}

[Serializable, NetSerializable]
public sealed partial class ChangePillBottleColorMessage : BoundUserInterfaceMessage
{
    public PillbottleColor NewColor;

    public ChangePillBottleColorMessage(PillbottleColor newColor)
    {
        NewColor = newColor;
    }
}

[Serializable, NetSerializable]
public enum ChangePillBottleUIKey
{
    Key
};

[Serializable, NetSerializable]
public enum PillBottleVisuals
{
    Color
};

[Serializable, NetSerializable]
public enum PillbottleColor : Byte
{
    Orange = 0,
    Blue,
    Yellow,
    Light_Purple,
    Light_Grey,
    White,
    Light_Green,
    Cyan,
    Bordeaux,
    Aquamarine,
    Grey,
    Red,
    Black
}
