using System.Diagnostics;
using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.Weather;
using Robust.Shared.Console;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using System.Linq;
using Content.Shared._RMC14.Light;
using Content.Shared.Dataset;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._RMC14.Light;

public sealed class RMCAmbientLightSystem : EntitySystem
{
    [Dependency] private readonly IConsoleHost _console = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;
    [Dependency] private readonly SharedRMCAmbientLightSystem _sharedLightSystem = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();
        // SubscribeLocalEvent<RMCAmbientLightSystem, ComponentGetState>(OnLightGetState);
        _console.RegisterCommand("rmclight",
            Loc.GetString("cmd-rmclight-desc"),
            Loc.GetString("cmd-rmclight-help"),
            RMCLight,
            WeatherCompletion);
    }

    // private void OnLightGetState(EntityUid uid, RMCAmbientLightSystem component, ref ComponentGetState args)
    // {
    //     args.State = new WeatherComponentState(component.Weather);
    // }

    [AdminCommand(AdminFlags.Fun)]
    private void RMCLight(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length < 2)
        {
            shell.WriteError(Loc.GetString("cmd-weather-error-no-arguments"));
            return;
        }

        // if (!int.TryParse(args[0], out var mapInt))
        //     return;

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
        var colors = colorDataset.Values.ToList();


        //Time parsing
        TimeSpan duration = TimeSpan.FromSeconds(30);
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

        SetColor((gridUid, rmcLight), colors, duration);
    }

    public void SetColor(Entity<RMCAmbientLightComponent> ent, string colorHex, TimeSpan duration)
    {
        var mapLight = EnsureComp<MapLightComponent>(ent);

        ent.Comp.Colors = [mapLight.AmbientLightColor, Color.FromHex(colorHex, Color.Black)];
        ent.Comp.Duration = duration;
        ent.Comp.StartTime = _timing.CurTime;
        ent.Comp.Running = true;

        Dirty(ent);
    }

    public void SetColor(Entity<RMCAmbientLightComponent> ent, List<string> colorHexes, TimeSpan duration)
    {
        if (colorHexes.Count == 0 || duration <= TimeSpan.Zero)
            return;

        var mapLight = EnsureComp<MapLightComponent>(ent);

        ent.Comp.Colors = colorHexes.Select(hex => Color.FromHex(hex, Color.Black)).ToList();
        ent.Comp.Duration = duration;
        ent.Comp.StartTime = _timing.CurTime;
        ent.Comp.Running = true;

        Dirty(ent);
    }

    private CompletionResult WeatherCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
            return CompletionResult.FromHintOptions(CompletionHelper.MapIds(EntityManager), "Map Id");

        var a = CompletionHelper.PrototypeIDs<DatasetPrototype>(true, _prototype);
        var b = a.Concat(new[] { new CompletionOption("null", Loc.GetString("cmd-weather-null")) });
        return CompletionResult.FromHintOptions(b, Loc.GetString("cmd-weather-hint"));
    }
}
