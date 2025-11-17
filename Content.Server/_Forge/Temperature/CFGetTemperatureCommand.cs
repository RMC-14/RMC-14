using Content.Server.Administration;
using Content.Server.Atmos.Components;
using Content.Shared._Forge.Temperature;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.Map;

namespace Content.Server._Forge.Temperature;

[AdminCommand(AdminFlags.Host)]
public sealed class CFGetTemperatureCommand : LocalizedEntityCommands
{
    public override string Command => "cfgettemperature";

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
            return CompletionResult.FromHintOptions(CompletionHelper.MapIds(EntityManager), "Map Id");

        return CompletionResult.Empty;
    }

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1)
        {
            shell.WriteError(LocalizationManager.GetString("command-cfgettemperature-usage"));
            return;
        }

        if (!TryParseMapId(args[0], out var mapId))
        {
            shell.WriteError(LocalizationManager.GetString("command-cfgettemperature-invalid-map"));
            return;
        }

        if (!EntityManager.System<SharedMapSystem>().TryGetMap(mapId, out var mapUidNullable) || mapUidNullable is not EntityUid mapEntity)
        {
            shell.WriteError(LocalizationManager.GetString("command-cfgettemperature-invalid-map"));
            return;
        }

        CFTemperatureControllerComponent? tempCtrl;
        MapAtmosphereComponent? atm;

        if (!EntityManager.TryGetComponent(mapEntity, out tempCtrl) ||
            !EntityManager.TryGetComponent(mapEntity, out atm))
        {
            shell.WriteError(LocalizationManager.GetString("command-cfgettemperature-no-controller"));
            return;
        }

        var kelvin = atm.Mixture.Temperature;
        var celsius = kelvin - 273.15f;

        shell.WriteLine(LocalizationManager.GetString("command-cfgettemperature-success",
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
