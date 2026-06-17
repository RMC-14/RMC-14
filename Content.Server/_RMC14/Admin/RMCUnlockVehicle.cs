using System.Linq;
using Content.Server.Administration;
using Content.Shared._RMC14.Intel.Tech;
using Content.Shared.Administration;
using Content.Shared.Prototypes;
using Content.Shared.Vehicle.Components;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;

namespace Content.Server._RMC14.Admin;

[AdminCommand(AdminFlags.Fun)]
public sealed class RMCUnlockVehicle : LocalizedEntityCommands
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    public override string Command => "unlockvehicle";


    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1)
        {
            shell.WriteError("Wrong number of args.");
            return;
        }

        if (!_prototype.TryIndex<EntityPrototype>(args[0], out var proto) ||
            !proto.HasComponent<VehicleComponent>() ||
            proto.Abstract)
            return;

        EntityManager.EventBus.RaiseEvent(EventSource.Local, new TechUnlockVehicleEvent(proto.ID));
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            return CompletionResult.FromHintOptions(
                _prototype.EnumeratePrototypes<EntityPrototype>()
                    .Where((ent) => ent.HasComponent<VehicleComponent>())
                    .Where((ent) => !ent.Abstract)
                    .Select((ent) => ent.ID),
                Loc.GetString("cmd-unlockvehicle-hint"));
        }

        return CompletionResult.Empty;
    }
}
