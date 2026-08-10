using Content.Server.Administration;
using Content.Server.Database;
using Content.Shared.Administration;
using Content.Shared.Roles;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Toolshed;
using Robust.Shared.Toolshed.Errors;
using Robust.Shared.Toolshed.TypeParsers;

namespace Content.Server._RMC14.PlayTimeTracking;

[ToolshedCommand, AdminCommand(AdminFlags.Ban)]
public sealed partial class ExcludeRoleTimerCommand : ToolshedCommand
{
    [Dependency] private IServerDbManager _db = default!;
    [Dependency] private RMCPlayTimeManager _playTime = default!;
    [Dependency] private IPlayerLocator _playerLocator = default!;
    [Dependency] private IPrototypeManager _prototypes = default!;

    [CommandImplementation("add")]
    public async void Add(
        [CommandInvocationContext] IInvocationContext ctx,
        [CommandArgument] ToolshedPlayer player,
        [CommandArgument] ProtoId<JobPrototype> tracker)
    {
        try
        {
            var found = await _playerLocator.LookupIdByNameOrIdAsync(player.Name);
            if (found == null)
            {
                ctx.WriteLine($"No player found with name {player.Name}");
                return;
            }

            var excluded = await _playTime.Exclude(found.UserId, tracker.Id);
            var jobName = _prototypes.Index(tracker).LocalizedName;
            ctx.WriteLine(!excluded
                ? $"Player {player} was already excluded from the playtime requirements for {jobName}"
                : $"Excluded player {player} from playtime requirements for {jobName}");
        }
        catch (Exception e)
        {
            ctx.WriteError(new UnhandledExceptionError(e));
        }
    }

    [CommandImplementation("get")]
    public async void Get(
        [CommandInvocationContext] IInvocationContext ctx,
        [CommandArgument] ToolshedPlayer player)
    {
        try
        {
            var found = await _playerLocator.LookupIdByNameOrIdAsync(player.Name);
            if (found == null)
            {
                ctx.WriteLine($"No player found with name {player.Name}");
                return;
            }

            var excluded = string.Join(", ", await _db.GetExcludedRoleTimers(found.UserId, default));
            if (string.IsNullOrWhiteSpace(excluded))
                excluded = "no roles";
            ctx.WriteLine($"Player {player} is excluded from playtime requirements for {excluded}");
        }
        catch (Exception e)
        {
            ctx.WriteError(new UnhandledExceptionError(e));
        }
    }

    [CommandImplementation("remove")]
    public async void Remove(
        [CommandInvocationContext] IInvocationContext ctx,
        [CommandArgument] ToolshedPlayer player,
        [CommandArgument] ProtoId<JobPrototype> tracker)
    {
        try
        {
            var found = await _playerLocator.LookupIdByNameOrIdAsync(player.Name);
            if (found == null)
            {
                ctx.WriteLine($"No player found with name {player.Name}");
                return;
            }

            var removed = await _playTime.RemoveRoleTimerExclusion(found.UserId, tracker.Id);
            ctx.WriteLine(removed
                ? $"Removed {player}'s playtime requirement exclusion for {_prototypes.Index(tracker).LocalizedName}"
                : $"Player {player} had no playtime requirement exclusion for {_prototypes.Index(tracker).LocalizedName}");
        }
        catch (Exception e)
        {
            ctx.WriteError(new UnhandledExceptionError(e));
        }
    }
}
