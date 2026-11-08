using Content.Client.Administration.Managers;
using Robust.Shared.Console;
using Robust.Shared.IoC;

namespace Content.Client._RMC14.Vehicle;

public sealed class VehicleOverlayCommands : EntitySystem
{
    [Dependency] private readonly IClientAdminManager _adminManager = default!;
    [Dependency] private readonly IConsoleHost _console = default!;
    [Dependency] private readonly GridVehicleMoverSystem _vehicleMover = default!;

    public override void Initialize()
    {
        _console.RegisterCommand("rmc_vehicle_debug", ToggleDebug);
        _console.RegisterCommand("rmc_vehicle_hardpoint", ToggleHardpoint);
        _console.RegisterCommand("rmc_vehicle_collision", ToggleCollision);
        _console.RegisterCommand("rmc_vehicle_movement", ToggleMovement);
    }

    public override void Shutdown()
    {
        _console.UnregisterCommand("rmc_vehicle_debug");
        _console.UnregisterCommand("rmc_vehicle_hardpoint");
        _console.UnregisterCommand("rmc_vehicle_collision");
        _console.UnregisterCommand("rmc_vehicle_movement");
    }

    private void ToggleDebug(IConsoleShell shell, string argStr, string[] args)
    {
        if (!_adminManager.IsAdmin())
        {
            shell.WriteError("You must be an admin to use this command.");
            return;
        }

        var enabled = _vehicleMover.ToggleDebugOverlay();
        shell.WriteLine($"Vehicle debug overlay: {(enabled ? "enabled" : "disabled")}");
    }

    private void ToggleHardpoint(IConsoleShell shell, string argStr, string[] args)
    {
        if (!_adminManager.IsAdmin())
        {
            shell.WriteError("You must be an admin to use this command.");
            return;
        }

        var enabled = _vehicleMover.ToggleHardpointOverlay();
        shell.WriteLine($"Vehicle hardpoint overlay: {(enabled ? "enabled" : "disabled")}");
    }

    private void ToggleCollision(IConsoleShell shell, string argStr, string[] args)
    {
        if (!_adminManager.IsAdmin())
        {
            shell.WriteError("You must be an admin to use this command.");
            return;
        }

        var enabled = _vehicleMover.ToggleCollisionOverlay();
        shell.WriteLine($"Vehicle collision overlay: {(enabled ? "enabled" : "disabled")}");
    }

    private void ToggleMovement(IConsoleShell shell, string argStr, string[] args)
    {
        if (!_adminManager.IsAdmin())
        {
            shell.WriteError("You must be an admin to use this command.");
            return;
        }

        var enabled = _vehicleMover.ToggleMovementOverlay();
        shell.WriteLine($"Vehicle movement overlay: {(enabled ? "enabled" : "disabled")}");
    }
}
