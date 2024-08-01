using Content.Shared.Alert;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Atmos;

public abstract class SharedRMCFlammableSystem : EntitySystem
{
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly INetManager _net = default!;

    private static readonly ProtoId<AlertPrototype> FireAlert = "Fire";

    public void UpdateFireAlert(EntityUid ent)
    {
        // for some reason flammable is in server WOOPEE
        if (_net.IsClient)
            return;

        var ev = new ShowFireAlertEvent();
        RaiseLocalEvent(ent, ref ev);

        if (ev.Show)
            _alerts.ShowAlert(ent, FireAlert);
        else
            _alerts.ClearAlert(ent, FireAlert);
    }
}
