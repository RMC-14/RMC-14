using System.Linq;
using Content.Server._RMC14.Admin;
using Content.Server.Administration;
using Content.Shared._RMC14.Marines;
using Content.Shared.Administration;
using Content.Shared.Roles;
using Robust.Shared.Toolshed;
using Robust.Shared.Toolshed.TypeParsers;

namespace Content.Server._RMC14.Marines;

[ToolshedCommand, AdminCommand(AdminFlags.Fun)]
public sealed class MarineCommand : ToolshedCommand
{
    [CommandImplementation("randomize")]
    public EntityUid Randomize([PipedArgument] EntityUid ent)
    {
        if (!HasComp<MarineComponent>(ent))
            return ent;

        var randomized = GetSys<RMCAdminSystem>().RandomizeMarine(ent);
        Del(ent);
        EnsureComp<MarineComponent>(randomized);
        return randomized;
    }

    [CommandImplementation("randomize")]
    public IEnumerable<EntityUid> Randomize([PipedArgument] IEnumerable<EntityUid> ents)
    {
        return ents.Select(Randomize);
    }

    // TODO RMC14 toolshed doesn't support implicit prototype kinds
    // [CommandImplementation("randomizewithgear")]
    // public EntityUid RandomizeGear(
    //     [PipedArgument] EntityUid ent,
    //     [CommandArgument] Prototype<StartingGearPrototype> gear)
    // {
    //     if (!HasComp<MarineComponent>(ent))
    //         return ent;
    //
    //     var randomized = GetSys<RMCAdminSystem>().RandomizeMarine(ent, gear: gear.Value);
    //     Del(ent);
    //     EnsureComp<MarineComponent>(randomized);
    //     return ent;
    // }
    //
    // [CommandImplementation("randomizewithgear")]
    // public IEnumerable<EntityUid> RandomizeGear(
    //     [PipedArgument] IEnumerable<EntityUid> ents,
    //     [CommandArgument] Prototype<StartingGearPrototype> gear)
    // {
    //     return ents.Select(e => RandomizeGear(e, gear));
    // }

    [CommandImplementation("randomizewithjob")]
    public EntityUid RandomizeJob(
        [PipedArgument] EntityUid ent,
        [CommandArgument] Prototype<JobPrototype> job)
    {
        if (!HasComp<MarineComponent>(ent))
            return ent;

        var randomized = GetSys<RMCAdminSystem>().RandomizeMarine(ent, job: job.Value);
        Del(ent);
        EnsureComp<MarineComponent>(randomized);
        return ent;
    }

    [CommandImplementation("randomizewithjob")]
    public IEnumerable<EntityUid> RandomizeJob(
        [PipedArgument] IEnumerable<EntityUid> ents,
        [CommandArgument] Prototype<JobPrototype> job)
    {
        return ents.Select(e => RandomizeJob(e, job));
    }
}
