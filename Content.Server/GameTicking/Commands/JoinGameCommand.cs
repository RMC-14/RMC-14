using Content.Server.Administration.Managers;
using Content.Server.Station.Systems;
using Content.Shared.Administration;
using Content.Shared.CCVar;
using Content.Shared.GameTicking;
using Content.Shared.Roles;
using Robust.Shared.Configuration;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;

namespace Content.Server.GameTicking.Commands
{
    [AnyCommand]
    sealed class JoinGameCommand : IConsoleCommand
    {
        [Dependency] private readonly IEntityManager _entManager = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IAdminManager _adminManager = default!;
        [Dependency] private readonly IConfigurationManager _cfg = default!;

        public string Command => "joingame";
        public string Description => "";
        public string Help => "joingame <jobId> <stationNetId> OR joingame <slot> <jobId> <stationNetId>";

        public JoinGameCommand()
        {
            IoCManager.InjectDependencies(this);
        }
        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length is not (2 or 3))
            {
                shell.WriteError(Loc.GetString("shell-wrong-arguments-number"));
                return;
            }

            var player = shell.Player;

            if (player == null)
            {
                return;
            }

            var ticker = _entManager.System<GameTicker>();
            var stationJobs = _entManager.System<StationJobsSystem>();

            if (ticker.PlayerGameStatuses.TryGetValue(player.UserId, out var status) && status == PlayerGameStatus.JoinedGame)
            {
                Logger.InfoS("security", $"{player.Name} ({player.UserId}) attempted to latejoin while in-game.");
                shell.WriteError($"{player.Name} is not in the lobby.   This incident will be reported.");
                return;
            }

            if (ticker.RunLevel == GameRunLevel.PreRoundLobby)
            {
                shell.WriteLine("Round has not started.");
                return;
            }
            else if (ticker.RunLevel == GameRunLevel.InRound)
            {
                // Optional first argument is character slot from the multi-slot late-join UI.
                int? selectedCharacterSlot = null;
                var argOffset = 0;
                if (args.Length == 3)
                {
                    if (!int.TryParse(args[0], out var parsedSlot))
                    {
                        shell.WriteError(Loc.GetString("shell-argument-must-be-number"));
                        return;
                    }

                    selectedCharacterSlot = parsedSlot;
                    argOffset = 1;
                }

                var id = args[argOffset];

                if (!int.TryParse(args[argOffset + 1], out var sid))
                {
                    shell.WriteError(Loc.GetString("shell-argument-must-be-number"));
                    return;
                }

                var station = _entManager.GetEntity(new NetEntity(sid));
                if (!_prototypeManager.TryIndex<JobPrototype>(id, out var jobPrototype))
                {
                    shell.WriteError(Loc.GetString("shell-argument-must-be-prototype"));
                    return;
                }

                if(stationJobs.TryGetJobSlot(station, jobPrototype, out var slots) == false || slots == 0)
                {
                    shell.WriteLine($"{jobPrototype.LocalizedName} has no available slots.");
                    return;
                }

                if (_adminManager.IsAdmin(player) && _cfg.GetCVar(CCVars.AdminDeadminOnJoin))
                {
                    _adminManager.DeAdmin(player);
                }

                ticker.MakeJoinGame(player, station, id, selectedCharacterSlot: selectedCharacterSlot);
                return;
            }

            ticker.MakeJoinGame(player, EntityUid.Invalid);
        }
    }
}
