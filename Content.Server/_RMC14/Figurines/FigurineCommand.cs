using System.IO;
using System.Linq;
using System.Reflection;
using Content.Server.Administration;
using Content.Server.Maps;
using Content.Server.Station.Components;
using Content.Shared._RMC14.Figurines;
using Content.Shared.Administration;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;
using Robust.Shared.Toolshed;

namespace Content.Server._RMC14.Figurines;

[ToolshedCommand, AdminCommand(AdminFlags.Host)]
public sealed class FigurineCommand : ToolshedCommand
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    [CommandImplementation("openslots")]
    public void OpenSlots()
    {
        var jobs = _prototype.EnumeratePrototypes<JobPrototype>().ToArray();

        foreach (var map in _prototype.EnumeratePrototypes<GameMapPrototype>())
        {
            foreach (var config in map.Stations.Values)
            {
                foreach (var entry in config.StationComponentOverrides.Values)
                {
                    if (entry.Component is not StationJobsComponent jobsComp)
                        continue;

                    foreach (var job in jobs)
                    {
#pragma warning disable RA0002
                        jobsComp.SetupAvailableJobs[job.ID] = [-1, -1];
#pragma warning restore RA0002
                    }
                }
            }
        }
    }

    [CommandImplementation("export")]
    public void Export([CommandInvocationContext] IInvocationContext ctx, [CommandArgument] string userId)
    {
        if (ctx.Session?.AttachedEntity is not { } ent)
        {
            ctx.WriteLine("You have no entity! Join the game.");
            return;
        }

        var ev = new FigurineRequestEvent();
        EntityManager.EntityNetManager.SendSystemNetworkMessage(ev, ctx.Session.Channel);

        var figurine = GetSys<FigurineSystem>();
        var name = MetaData(ent).EntityName;
        var yaml = @$"
- type: entity
  parent: RMCBaseFigurinePatron
  id: RMCFigurinePatron{figurine.FormatId(name)}
  name: {name} figurine
  description: """"
  components:
  - type: Sprite
    state: {figurine.FormatSpriteName(name)}
  - type: PatronFigurine
    id: {userId}
";

        var resources = figurine.GetResourcesPath();
        var prototypes = Path.Combine(resources, "Prototypes/_RMC14/Entities/Objects/patron_figurines.yml");

        File.AppendAllText(prototypes, yaml);
    }
}
