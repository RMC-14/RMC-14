using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Xenonids.Egg;

[ImplicitDataDefinitionForInheritors, Serializable, NetSerializable]
public sealed partial class XenoEggPlasmaHiveData : ReagentData
{
    [DataField]
    public NetEntity? Hive;

    public override ReagentData Clone() => this;

    public override bool Equals(ReagentData? other)
    {
        return other is XenoEggPlasmaHiveData data && data.Hive == Hive;
    }

    public override int GetHashCode()
    {
        return Hive.GetHashCode();
    }
}
