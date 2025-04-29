using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Shared.Console;
using System.Linq;
using Content.Shared._RMC14.Light;
using Content.Shared.Dataset;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;

namespace Content.Server._RMC14.Light;

public sealed class RMCAmbientLightCommand : EntitySystem
{
    [Dependency] private readonly IConsoleHost _console = default!;
    [Dependency] private readonly RMCAmbientLightSystem _lightSystem = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    public override void Initialize()
    {
        base.Initialize();
        _console.RegisterCommand("rmclightsequence",
            Loc.GetString("cmd-rmclight-desc"),
            Loc.GetString("cmd-rmclight-help"),
            RMCLightSequence,
            RMCLightSequenceCompletion);
        _console.RegisterCommand("rmclight",
            Loc.GetString("cmd-rmclight-desc"),
            Loc.GetString("cmd-rmclight-help"),
            RMCLight,
            RMCLightCompletion);
    }

    [AdminCommand(AdminFlags.Fun)]
    private void RMCLight(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length < 2)
        {
            shell.WriteError(Loc.GetString("cmd-weather-error-no-arguments"));
            return;
        }

        if (!EntityUid.TryParse(args[0], out var gridUid))
            return;

        _entityManager.EnsureComponent<RMCAmbientLightComponent>(gridUid, out var rmcLight);

        //Color Proto parsing
        Color colorHex = default;
        if (!args[1].Equals("null"))
        {
            if (!Color.TryParse(args[1], out colorHex))
            {
                shell.WriteError(Loc.GetString("cmd-weather-error-unknown-proto"));
                return;
            }
        }

        //Time parsing
        var duration = TimeSpan.FromSeconds(30);
        if (args.Length == 3)
        {
            if (int.TryParse(args[2], out var durationInt))
            {
                duration = TimeSpan.FromSeconds(durationInt);
            }
            else
            {
                shell.WriteError(Loc.GetString("cmd-weather-error-wrong-time"));
            }
        }

        _lightSystem.SetColor((gridUid, rmcLight), colorHex, duration);
    }

    private CompletionResult RMCLightCompletion(IConsoleShell shell, string[] args)
    {
        return args.Length switch
        {
            1 => CompletionResult.FromHintOptions(CompletionHelper.Components<MapGridComponent>(args[0], _entityManager), "Grid Id"),
            2 => CompletionResult.FromHintOptions(CompletionHelper.PrototypeIDs<DatasetPrototype>(true, _prototype), Loc.GetString("ColorHex")),
            3 => CompletionResult.FromHint(Loc.GetString("duration")),
            _ => CompletionResult.Empty,
        };
    }

    [AdminCommand(AdminFlags.Fun)]
    private void RMCLightSequence(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length < 2)
        {
            shell.WriteError(Loc.GetString("cmd-weather-error-no-arguments"));
            return;
        }

        if (!EntityUid.TryParse(args[0], out var gridUid))
            return;

        _entityManager.EnsureComponent<RMCAmbientLightComponent>(gridUid, out var rmcLight);

        //Color Proto parsing
        DatasetPrototype? colorDataset = null;
        if (!args[1].Equals("null"))
        {
            if (!_prototype.TryIndex(args[1], out colorDataset))
            {
                shell.WriteError(Loc.GetString("cmd-weather-error-unknown-proto"));
                return;
            }
        }

        if (colorDataset == null)
            return;
        var colors = colorDataset.Values.Select(hex => Color.FromHex(hex, Color.Black)).ToList();

        //Time parsing
        var duration = TimeSpan.FromSeconds(30);
        if (args.Length == 3)
        {
            if (int.TryParse(args[2], out var durationInt))
            {
                duration = TimeSpan.FromSeconds(durationInt);
            }
            else
            {
                shell.WriteError(Loc.GetString("cmd-weather-error-wrong-time"));
            }
        }

        _lightSystem.SetColor((gridUid, rmcLight), colors, duration);
    }

    private CompletionResult RMCLightSequenceCompletion(IConsoleShell shell, string[] args)
    {
        return args.Length switch
        {
            1 => CompletionResult.FromHintOptions(CompletionHelper.Components<MapGridComponent>(args[0], _entityManager), "Grid Id"),
            2 => CompletionResult.FromHintOptions(CompletionHelper.PrototypeIDs<DatasetPrototype>(true, _prototype), Loc.GetString("ColorSequence")),
            3 => CompletionResult.FromHint(Loc.GetString("duration")),
            _ => CompletionResult.Empty,
        };
    }
}
