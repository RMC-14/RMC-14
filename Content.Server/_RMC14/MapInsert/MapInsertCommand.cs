using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Shared.Toolshed;

namespace Content.Server._RMC14.MapInsert;

[ToolshedCommand, AdminCommand(AdminFlags.VarEdit)]
public sealed class SpawnAllMapInsertsCommand : ToolshedCommand
{
    [CommandImplementation]
    public void Run(IInvocationContext ctx)
    {
        if (ExecutingEntity(ctx) is not { } entity)
            return;

        var mapInsertSystem = IoCManager.Resolve<IEntityManager>().System<MapInsertSystem>();
        var mapInsertQuery = EntityManager.EntityQueryEnumerator<MapInsertComponent>();
        EntityUid mapInsertEntity = default;
        while (mapInsertQuery.MoveNext(out var uid, out var mapInsert))
        {
            mapInsertEntity = uid;
            mapInsertSystem.ProcessMapInsert((uid, mapInsert), true);
        }

        if (mapInsertEntity == default)
        {
            ctx.WriteLine("No map inserts found. Ensure that you loaded a map with map inserts and initialized it.");
            return;
        }

        ctx.WriteLine("Spawning all map inserts.");

    }
}
