using Content.Server.Administration;
using Content.Server.GameTicking;
using Content.Shared._RMC14.Rules;
using Content.Shared.Administration;
using Robust.Shared.Prototypes;
using Robust.Shared.Toolshed;
using Robust.Shared.Utility;

namespace Content.Server._RMC14.Rules;

[ToolshedCommand, AdminCommand(AdminFlags.Round)]
public sealed class ForcePlanetMapCommand : ToolshedCommand
{
    [CommandImplementation]
    public void Run(IInvocationContext ctx, EntProtoId<RMCPlanetMapPrototypeComponent> planet)
    {
        if (Sys<GameTicker>().RunLevel != GameRunLevel.PreRoundLobby)
        {
            ctx.WriteLine("This command can only be run in the lobby!");
            return;
        }

        if (!Sys<RMCPlanetSystem>().GetAllPlanets().TryFirstOrNull(p => p.Proto.ID == planet, out var first))
        {
            ctx.WriteLine($"No planet found with id {planet.Id}");
            return;
        }

        var planetSys = Sys<CMDistressSignalRuleSystem>();
        planetSys.CancelPlanetVote();
        planetSys.SetPlanet(first.Value);
        ctx.WriteLine($"The next round's planet has been set to {first.Value}");
    }
}
