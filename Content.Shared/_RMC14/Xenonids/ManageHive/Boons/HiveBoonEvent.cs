using Content.Shared._RMC14.Xenonids.Hive;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Xenonids.ManageHive.Boons;

[DataDefinition]
[ImplicitDataDefinitionForInheritors]
[Serializable, NetSerializable]
public partial class HiveBoonEvent
{
    [NonSerialized]
    public Entity<HiveComponent> Hive;

    [DataField]
    public bool Activated;
}
