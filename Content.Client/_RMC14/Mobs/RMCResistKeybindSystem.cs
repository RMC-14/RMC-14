using Content.Client.Alerts;
using Content.Shared._RMC14.Input;
using Content.Shared.Alert;
using Robust.Shared.Input.Binding;
using Robust.Shared.Prototypes;

namespace Content.Client._RMC14.Mobs;

public sealed class RMCResistKeybindSystem : EntitySystem
{
    [Dependency] private readonly ClientAlertsSystem _alerts = default!;

    private static readonly ProtoId<AlertPrototype>[] ResistAlerts =
    [
        "Fire",
        "Handcuffed",
        "Ensnared",
        "Buckled",
        "Pulled",
    ];

    public override void Initialize()
    {
        base.Initialize();

        CommandBinds.Builder
            .Bind(CMKeyFunctions.RMCResist,
                InputCmdHandler.FromDelegate(session =>
                    {
                        if (session?.AttachedEntity is not { } ent)
                            return;

                        foreach (var alertId in ResistAlerts)
                        {
                            if (!_alerts.IsShowingAlert(ent, alertId))
                                continue;

                            _alerts.AlertClicked(alertId);
                        }
                    },
                    handle: true))
            .Register<RMCResistKeybindSystem>();
    }

    public override void Shutdown()
    {
        base.Shutdown();
        CommandBinds.Unregister<RMCResistKeybindSystem>();
    }
}
