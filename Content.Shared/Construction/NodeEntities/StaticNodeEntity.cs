using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Shared.Construction.NodeEntities;

[UsedImplicitly]
[DataDefinition]
public sealed partial class StaticNodeEntity : IGraphNodeEntity
{
    [DataField("id")]
    [ViewVariables(VVAccess.ReadWrite)]
    public EntProtoId? Id { get; private set; }

    public StaticNodeEntity()
    {
    }

    public StaticNodeEntity(string id)
    {
        Id = id;
    }

    public string? GetId(EntityUid? uid, EntityUid? userUid, GraphNodeEntityArgs args)
    {
        return Id?.Id;
    }
}
