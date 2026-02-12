using Content.Shared._RMC14.CCVar;
using Content.Shared.Movement.Components;
using Robust.Shared.Configuration;
using Robust.Shared.Map.Components;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Shared._RMC14.Input;

public sealed class RMCInputSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly INetManager _net = default!;

    private bool _activeInputMoverEnabled;

    private EntityQuery<ActorComponent> _actorQuery;

    public override void Initialize()
    {
        _actorQuery = GetEntityQuery<ActorComponent>();

        SubscribeLocalEvent<ActiveInputMoverComponent, MapInitEvent>(OnActiveMapInit);
        SubscribeLocalEvent<ActiveInputMoverComponent, PlayerAttachedEvent>(OnActiveAttached);
        SubscribeLocalEvent<ActiveInputMoverComponent, PlayerDetachedEvent>(OnActiveDetached);

        // Clean up RelativeEntity refs when grids/maps are deleted.
        SubscribeLocalEvent<MapGridComponent, EntityTerminatingEvent>(OnGridOrMapTerminating);
        SubscribeLocalEvent<MapComponent, EntityTerminatingEvent>(OnGridOrMapTerminating);

        Subs.CVar(_config, RMCCVars.RMCActiveInputMoverEnabled, v => _activeInputMoverEnabled = v, true);
    }

    private void OnGridOrMapTerminating(EntityUid uid, Component comp, ref EntityTerminatingEvent args)
    {
        var query = EntityQueryEnumerator<InputMoverComponent>();
        while (query.MoveNext(out _, out var mover))
        {
            if (mover.RelativeEntity == uid)
                mover.RelativeEntity = null;
        }
    }

    private void OnActiveMapInit(Entity<ActiveInputMoverComponent> ent, ref MapInitEvent args)
    {
        if (!_activeInputMoverEnabled || _net.IsClient)
            return;

        if (_actorQuery.HasComp(ent))
            EnsureComp<InputMoverComponent>(ent);
        else
            RemCompDeferred<InputMoverComponent>(ent);
    }

    private void OnActiveAttached(Entity<ActiveInputMoverComponent> ent, ref PlayerAttachedEvent args)
    {
        if (!_activeInputMoverEnabled)
            return;

        EnsureComp<InputMoverComponent>(ent);
    }

    private void OnActiveDetached(Entity<ActiveInputMoverComponent> ent, ref PlayerDetachedEvent args)
    {
        if (!_activeInputMoverEnabled)
            return;

        RemCompDeferred<InputMoverComponent>(ent);
    }
}
