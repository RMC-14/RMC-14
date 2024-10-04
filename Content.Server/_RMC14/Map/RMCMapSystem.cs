using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Content.Shared._RMC14.Map;
using Robust.Server.GameObjects;
using Robust.Server.Maps;
using Robust.Shared.Map;

namespace Content.Server._RMC14.Map;

public sealed class RMCMapSystem : SharedRMCMapSystem
{
    [Dependency] private readonly MapLoaderSystem _mapLoader = default!;

    public override bool TryLoad(
        MapId mapId,
        string path,
        [NotNullWhen(true)] out IReadOnlyList<EntityUid>? ents,
        Matrix3x2? transform = null)
    {
        var mapLoadOptions = transform == null ? null : new MapLoadOptions { TransformMatrix = transform.Value };
        return _mapLoader.TryLoad(mapId, path, out ents, mapLoadOptions);
    }
}
