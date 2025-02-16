using Content.Shared._RMC14.Weapons.Ranged.Auto;
using Robust.Client.Graphics;
using Robust.Shared.Console;

namespace Content.Client._RMC14.Weapons.Ranged.Auto;

public sealed class ShowAutoFireSystem : EntitySystem
{
    [Dependency] private readonly IConsoleHost _console = default!;
    [Dependency] private readonly GunToggleableAutoFireSystem _autoFire = default!;
    [Dependency] private readonly IOverlayManager _overlay = default!;

    public override void Initialize()
    {
        _console.RegisterCommand("showautofire", Loc.GetString("cmd-showautofire-desc"), Loc.GetString("cmd-showautofire-help"), ShowAutoFireCommand);
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _console.UnregisterCommand("showautofire");
        _overlay.RemoveOverlay<ShowAutoFireOverlay>();
    }

    private void ShowAutoFireCommand(IConsoleShell shell, string argstr, string[] args)
    {
        if (!_overlay.RemoveOverlay<ShowAutoFireOverlay>())
        {
            _autoFire.Debug = true;
            _overlay.AddOverlay(new ShowAutoFireOverlay());
            shell.WriteLine(Loc.GetString("cmd-showautofire-enabled"));
            return;
        }

        _autoFire.Debug = false;
        shell.WriteLine(Loc.GetString("cmd-showautofire-disabled"));
    }
}
