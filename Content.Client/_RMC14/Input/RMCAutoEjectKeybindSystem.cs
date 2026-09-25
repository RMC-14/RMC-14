using Content.Shared._RMC14.CCVar;
using Content.Shared._RMC14.Input;
using Content.Shared.Popups;
using Robust.Shared.Configuration;
using Robust.Shared.Input.Binding;

namespace Content.Client._RMC14.Input;

public sealed class RMCAutoEjectKeybindSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _configuration = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        CommandBinds.Builder
            .Bind(CMKeyFunctions.RMCToggleAutoEject,
                InputCmdHandler.FromDelegate(session =>
                {
                    if (session?.AttachedEntity is { } user)
                        ToggleAutoEject(user);
                }, handle: false))
            .Register<RMCAutoEjectKeybindSystem>();
    }

    public override void Shutdown()
    {
        base.Shutdown();
        CommandBinds.Unregister<RMCAutoEjectKeybindSystem>();
    }

    public bool ToggleAutoEject(EntityUid user, bool save = true)
    {
        var enabled = !_configuration.GetCVar(RMCCVars.RMCAutoEjectMagazines);
        _configuration.SetCVar(RMCCVars.RMCAutoEjectMagazines, enabled);
        if (save)
            _configuration.SaveToFile();

        var message = Loc.GetString(enabled
            ? "rmc-keybind-auto-eject-enabled"
            : "rmc-keybind-auto-eject-disabled");
        _popup.PopupClient(message, user, user);
        return enabled;
    }
}
