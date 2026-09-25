using Content.Server.Administration;
using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server._RMC14.Xenonids.Hive;

[AdminCommand(AdminFlags.Debug)]
public sealed class RMCGenerateHiveSlotsCommand : IConsoleCommand
{
    [Dependency] private readonly IEntityManager _entity = default!;
    [Dependency] private readonly IEntitySystemManager _entitySystems = default!;

    public string Command => "rmcgeneratehiveslots";

    public string Description =>
        "Generates fixed hive slot roster (normal/corrupted/alpha/bravo/charlie/delta/renegade)";

    public string Help => "rmcgeneratehiveslots";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var system = _entitySystems.GetEntitySystem<RMCXenoHiveSlotSystem>();
        var created = system.GenerateSlots();

        if (created.Count == 0)
        {
            shell.WriteLine("Hive slots already exist this round, nothing generated. Existing slots:");
            ListSlots(shell);
            return;
        }

        shell.WriteLine($"Generated {created.Count} hive slot(s):");
        ListSlots(shell);
    }

    private void ListSlots(IConsoleShell shell)
    {
        var query = _entity.EntityQueryEnumerator<HiveSlotComponent>();
        while (query.MoveNext(out var uid, out var slot))
        {
            var name = _entity.GetComponent<MetaDataComponent>(uid).EntityName;
            shell.WriteLine($"  {uid} \"{name}\" position={slot.Position}");
        }
    }
}
