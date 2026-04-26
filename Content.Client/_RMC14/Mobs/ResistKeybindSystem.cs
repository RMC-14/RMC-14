using Content.Client.Alerts;
using Content.Shared._RMC14.Input;
using Content.Shared.Alert;
using Robust.Client.Player;
using Robust.Shared.Input.Binding;
using Robust.Shared.Prototypes;

namespace Content.Client._RMC14.Mobs;

/// <summary>
/// Handles the resist keybind, which activates the first applicable resist-related alert for the local player.
/// Priority order: Fire, Handcuffed, Ensnared, Buckled.
/// </summary>
public sealed class ResistKeybindSystem : EntitySystem
{
    [Dependency] private readonly ClientAlertsSystem _alerts = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    private static readonly ProtoId<AlertPrototype>[] ResistAlerts =
    [
        "Fire",
        "Handcuffed",
        "Ensnared",
        "Buckled",
    ];

    public override void Initialize()
    {
        base.Initialize();

        CommandBinds.Builder
            .Bind(CMKeyFunctions.RMCResist,
                InputCmdHandler.FromDelegate(_ =>
                {
                    var ent = _playerManager.LocalEntity;
                    if (ent == null || !ent.Value.IsValid())
                        return;

                    foreach (var alertId in ResistAlerts)
                    {
                        if (!_alerts.IsShowingAlert(ent.Value, alertId))
                            continue;

                        _alerts.AlertClicked(alertId);
                        return;
                    }
                },
                handle: true))
            .Register<ResistKeybindSystem>();
    }

    public override void Shutdown()
    {
        base.Shutdown();
        CommandBinds.Unregister<ResistKeybindSystem>();
    }
}
