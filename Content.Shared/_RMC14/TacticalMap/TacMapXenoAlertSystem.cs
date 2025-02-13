using Content.Shared._RMC14.Areas;
using Content.Shared.Alert;
using Content.Shared.Coordinates;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.TacticalMap;

public sealed class TacMapXenoAlertSystem : EntitySystem
{
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly AreaSystem _area = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<TacMapXenoAlertComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<TacMapXenoAlertComponent, ComponentRemove>(OnRemove);
    }

    private void OnMapInit(Entity<TacMapXenoAlertComponent> ent, ref MapInitEvent args)
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

    public override void Update(float frameTime)
    {
        if (_net.IsClient)
            return;

        var time = _timing.CurTime;
        var tacMapQuery = EntityQueryEnumerator<TacMapXenoAlertComponent>();

        while (tacMapQuery.MoveNext(out var uid, out var alert))
        {
            if (time < alert.NextUpdateTime)
                continue;

            _alerts.ShowAlert(uid, alert.Alert, dynamicMessage: Loc.GetString("rmc-tacmap-alert-area", ("area", GetAreaName(uid))));
            alert.NextUpdateTime += alert.UpdateInterval;
        }
    }
}
