using Content.Shared.Alert;

namespace Content.Shared._RMC14.TacticalMap;

public sealed class TacMapXenoAlertSystem : EntitySystem
{
    [Dependency] private readonly AlertsSystem _alerts = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<TacMapXenoAlertComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<TacMapXenoAlertComponent, ComponentRemove>(OnRemove);
    }

    private void OnMapInit(Entity<TacMapXenoAlertComponent> ent, ref MapInitEvent args)
    {
        _alerts.ShowAlert(ent, ent.Comp.Alert);
    }
    private void OnRemove(Entity<TacMapXenoAlertComponent> ent, ref ComponentRemove args)
    {
        _alerts.ClearAlert(ent, ent.Comp.Alert);
    }
}
