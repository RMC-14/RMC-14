﻿using Content.Server.Administration;
using Content.Shared._RMC14.WeedKiller;
using Content.Shared.Administration;
using Content.Shared.Coordinates;
using Robust.Shared.Toolshed;

namespace Content.Server._RMC14.WeedKiller;

[ToolshedCommand, AdminCommand(AdminFlags.VarEdit)]
public sealed class WeedKillerCommand : ToolshedCommand
{
    [CommandImplementation]
    public void Run(IInvocationContext ctx)
    {
        if (ExecutingEntity(ctx) is not { } entity)
            return;

        Sys<WeedKillerSystem>().CreateWeedKiller("dropship", entity.ToCoordinates());
        ctx.WriteLine("Running weed killer on the current location");
    }
}
