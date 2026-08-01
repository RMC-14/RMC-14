using System.Diagnostics.CodeAnalysis;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Areas;

public sealed class SharedAreaLookupSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypes = default!;

    public bool TryGetArea(
        EntityUid gridUid,
        Vector2i indices,
        [NotNullWhen(true)] out Entity<AreaComponent>? area,
        [NotNullWhen(true)] out EntityPrototype? areaPrototype)
    {
        area = default;
        areaPrototype = default;

        if (!TryComp(gridUid, out AreaGridComponent? areaGrid))
            return false;

        if (!areaGrid.Areas.TryGetValue(indices, out var areaProtoId))
            return false;

        if (!_prototypes.TryIndex(areaProtoId, out areaPrototype))
            return false;

        if (!areaGrid.AreaEntities.TryGetValue(areaProtoId, out var areaEnt) ||
            !TryComp(areaEnt, out AreaComponent? areaComp))
        {
            return false;
        }

        area = (areaEnt, areaComp);
        return true;
    }
}
