using System.Linq;
using System.Numerics;
using Content.Server.Decals;
using Content.Shared.Decals;
using Robust.Shared.Prototypes;

namespace Content.Server._RMC14.Decals;

public sealed class RMCDecalSystem : EntitySystem
{
    [Dependency] private readonly DecalSystem _decal = default!;

    public int GetDecalsInTile(EntityUid ent, IReadOnlyCollection<ProtoId<DecalPrototype>> decals)
    {
        var gridUid = Transform(ent).GridUid;
        if (!gridUid.HasValue)
            return 0;

        var tileBounds = Box2.CenteredAround(Transform(ent).Coordinates.Offset(new Vector2(-0.5f, -0.5f)).Position, Vector2.One);
        var tileDecals = _decal.GetDecalsIntersecting((EntityUid)gridUid, tileBounds);

        return tileDecals.Count(x => decals.Contains(x.Decal.Id));
    }
}
