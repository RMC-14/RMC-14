using System.Linq;
using Content.Server.Administration;
using Content.Shared._RMC14.GameStates;
using Content.Shared.Administration;
using Robust.Server.Player;
using Robust.Shared.Console;

namespace Content.Server._RMC14.GameStates;

public sealed class RMCGameStateSystem : EntitySystem
{
    [Dependency] private readonly IConsoleHost _console = default!;
    [Dependency] private readonly IPlayerManager _player = default!;

    public override void Initialize()
    {
        _console.RegisterCommand(
            "enableprediction",
            "Enables prediction for a player",
            "enableprediction <player>",
            EnablePrediction,
            EnablePredictionCompletions
        );

        _console.RegisterCommand(
            "disableprediction",
            "Disables prediction for a player",
            "disableprediction <player>",
            DisablePrediction,
            EnablePredictionCompletions
        );
    }

    [AdminCommand(AdminFlags.Debug)]
    private void EnablePrediction(IConsoleShell shell, string argStr, string[] args)
    {
        var name = string.Join(" ", args);
        if (!_player.TryGetSessionByUsername(name, out var player))
        {
            shell.WriteLine($"No player found with name {name}");
            return;
        }

        var ev = new RMCSetPredictionEvent(true);
        RaiseNetworkEvent(ev, player);
    }

    private CompletionResult EnablePredictionCompletions(IConsoleShell shell, string[] args)
    {
        if (args.Length != 1)
            return CompletionResult.Empty;

        var options = _player.Sessions.Select(c => c.Name).OrderBy(c => c).ToArray();
        return CompletionResult.FromHintOptions(options, Loc.GetString("cmd-ban-hint"));
    }

    [AdminCommand(AdminFlags.Debug)]
    private void DisablePrediction(IConsoleShell shell, string argStr, string[] args)
    {
        var name = string.Join(" ", args);
        if (!_player.TryGetSessionByUsername(name, out var player))
        {
            shell.WriteLine($"No player found with name {name}");
            return;
        }

        var ev = new RMCSetPredictionEvent(false);
        RaiseNetworkEvent(ev, player);
    }
}
