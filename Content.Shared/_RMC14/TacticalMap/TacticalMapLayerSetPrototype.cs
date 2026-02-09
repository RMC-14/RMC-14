using System.Collections.Generic;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.TacticalMap;

[Prototype(TacticalMapPrototypeIds.LayerSet), Serializable, NetSerializable]
public sealed partial class TacticalMapLayerSetPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public List<ProtoId<TacticalMapLayerPrototype>> Layers { get; private set; } = new();
}
