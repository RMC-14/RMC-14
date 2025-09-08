using Robust.Shared.Map;

namespace Content.Shared._RMC14.Marines;

public sealed class WarshipSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public bool TryGetWarshipMap(EntityUid reference, out MapId mapId)
    {
        var referenceMap = _transform.GetMap(reference);
        if (HasComp<AlmayerComponent>(referenceMap))
        {
            // If the entity we are referencing such as a console is on a map with an AlmayerComponent,
            // we assume that's the priority one
            // For example, if there are multiple Almayers with multiple communications consoles, this will
            // allow all of them to work properly
            mapId = _transform.GetMapId(reference);
            return true;
        }

        var query = EntityQueryEnumerator<AlmayerComponent, TransformComponent>();
        while (query.MoveNext(out _, out var xform))
        {
            mapId = xform.MapID;
            return true;
        }

        mapId = default;
        return false;
    }
}
