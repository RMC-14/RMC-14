using Content.Shared._RMC14.Areas;
using Content.Shared.Alert;
using Content.Shared.Coordinates;

namespace Content.Shared._RMC14.TacticalMap;

public sealed class TacMapXenoAlertSystem : EntitySystem
{
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly AreaSystem _area = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<TacMapXenoAlertComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<TacMapXenoAlertComponent, ComponentRemove>(OnRemove);
        SubscribeLocalEvent<TacMapXenoAlertComponent, MoveEvent>(OnMove);
    }

    private void OnMapInit(Entity<TacMapXenoAlertComponent> ent, ref MapInitEvent args)
    {
        _alerts.ShowAlert(ent, ent.Comp.Alert, dynamicMessage: Loc.GetString("rmc-tacmap-alert-area", ("area", GetAreaName(ent))));
    }
    private void OnMove(Entity<TacMapXenoAlertComponent> ent, ref MoveEvent args)
    {
        _alerts.ShowAlert(ent, ent.Comp.Alert, dynamicMessage: Loc.GetString("rmc-tacmap-alert-area", ("area", GetAreaName(ent))));
    }
    private void OnRemove(Entity<TacMapXenoAlertComponent> ent, ref ComponentRemove args)
    {
        _alerts.ClearAlert(ent, ent.Comp.Alert);
    }
    private string GetAreaName(EntityUid ent)
    {
        if (!_area.TryGetArea(ent.ToCoordinates(), out var _, out var areaProto))
            return Loc.GetString("rmc-tacmap-alert-no-area");

        return areaProto.Name;
    }
}
