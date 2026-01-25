using Content.Server.Administration;
using Content.Server.Administration.Systems;
using Content.Shared._RMC14.Marines;
using Content.Shared.Administration;
using Content.Shared.Coordinates;
using Content.Shared.Mind.Components;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.Toolshed;

namespace Content.Server._RMC14.Admin;

[ToolshedCommand, AdminCommand(AdminFlags.Admin)]
public sealed class RMCRejuvenateCommand : ToolshedCommand
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    [CommandImplementation("marineplayersinrange")]
    public void MarinePlayersInRange([CommandInvocationContext] IInvocationContext ctx, [CommandArgument] int range)
    {
        if (ctx.Session?.AttachedEntity is not { } ent)
        {
            ctx.WriteLine("You have no entity!");
            return;
        }

        if (range <= 0)
        {
            ctx.WriteLine($"Range needs to be a positive number, {range} was given.");
            return;
        }

        var rangeLimit = _cfg.GetCVar(CVars.ToolshedNearbyLimit);
        if (range > rangeLimit)
            throw new ArgumentException($"Tried to query too big of a range with nearby ({range})! Limit: {rangeLimit}. Change the {CVars.ToolshedNearbyLimit.Name} cvar to increase this at your own risk.");

        var lookup = Sys<EntityLookupSystem>();
        var rejuvenate = Sys<RejuvenateSystem>();
        var marines = lookup.GetEntitiesInRange<MarineComponent>(ent.ToCoordinates(), range);
        foreach (var marine in marines)
        {
            if (!TryComp(marine, out MindContainerComponent? mind) ||
                !mind.HasMind)
            {
                continue;
            }

            rejuvenate.PerformRejuvenate(marine);
        }
    }
}
