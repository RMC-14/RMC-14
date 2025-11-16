using Content.Server.Administration;
using Content.Server.Atmos.Components;
using Content.Shared._Forge.Temperature;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Map;

namespace Content.Server._Forge.Temperature;

[AdminCommand(AdminFlags.Host)]
public sealed class GetTemperatureCommand : IConsoleCommand
{
    public string Command => "gettemperature";
    public string Description => Loc.GetString("command-gettemperature-description");
    public string Help => Loc.GetString("command-gettemperature-help");

    public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
            return CompletionResult.FromHintOptions(CompletionHelper.MapIds(IoCManager.Resolve<IEntityManager>()), "Map Id");

        return CompletionResult.Empty;
    }

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1)
        {
            shell.WriteError(Loc.GetString("command-gettemperature-usage"));
            return;
        }

        if (!TryParseMapId(args[0], out var mapId))
        {
            shell.WriteError(Loc.GetString("command-gettemperature-invalid-map"));
            return;
        }

        var entManager = IoCManager.Resolve<IEntityManager>();

        if (!entManager.System<SharedMapSystem>().TryGetMap(mapId, out var mapUidNullable) || mapUidNullable is not EntityUid mapEntity)
        {
            shell.WriteError(Loc.GetString("command-gettemperature-invalid-map"));
            return;
        }

        TemperatureControllerComponent? tempCtrl;
        MapAtmosphereComponent? atm;

        if (!entManager.TryGetComponent(mapEntity, out tempCtrl) ||
            !entManager.TryGetComponent(mapEntity, out atm))
        {
            shell.WriteError(Loc.GetString("command-gettemperature-no-controller"));
            return;
        }

        var kelvin = atm.Mixture.Temperature;
        var celsius = kelvin - 273.15f;

        shell.WriteLine(Loc.GetString("command-gettemperature-success",
            ("kelvin", kelvin.ToString("0.##")),
            ("celsius", celsius.ToString("0.##")),
            ("zone", tempCtrl.Zone.ToString())));
    }

    private static bool TryParseMapId(string arg, out MapId mapId)
    {
        mapId = default;
        if (!int.TryParse(arg, out var id))
            return false;
        mapId = new MapId(id);
        return true;
    }
}
