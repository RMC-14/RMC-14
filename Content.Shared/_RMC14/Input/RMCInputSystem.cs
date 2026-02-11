using Content.Shared._RMC14.CCVar;
using Content.Shared.Movement.Components;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Shared._RMC14.Input;

public sealed class RMCInputSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly INetManager _net = default!;

    private bool _activeInputMoverEnabled;

    private EntityQuery<ActorComponent> _actorQuery;
    private EntityQuery<InputMoverComponent> _inputMoverQuery;
    private EntityQuery<InputMoverRelativeTargetComponent> _relativeTargetQuery;

    public override void Initialize()
    {
        _actorQuery = GetEntityQuery<ActorComponent>();
        _inputMoverQuery = GetEntityQuery<InputMoverComponent>();
        _relativeTargetQuery = GetEntityQuery<InputMoverRelativeTargetComponent>();

        SubscribeLocalEvent<ActiveInputMoverComponent, MapInitEvent>(OnActiveMapInit);
        SubscribeLocalEvent<ActiveInputMoverComponent, PlayerAttachedEvent>(OnActiveAttached);
        SubscribeLocalEvent<ActiveInputMoverComponent, PlayerDetachedEvent>(OnActiveDetached);
        SubscribeLocalEvent<InputMoverRelativeTargetComponent, EntityTerminatingEvent>(OnRelativeTargetTerminating);

        // Clear RelativeEntity references when one of the target entities is deleted
        Subs.CVar(_config, RMCCVars.RMCActiveInputMoverEnabled, v => _activeInputMoverEnabled = v, true);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<InputMoverComponent>();
        while (query.MoveNext(out var mover))
        {
            if (mover.RelativeEntity is { } relative && !_relativeTargetQuery.HasComp(relative))
                EnsureComp<InputMoverRelativeTargetComponent>(relative);
        }
    }

    private void OnRelativeTargetTerminating(Entity<InputMoverRelativeTargetComponent> ent, ref EntityTerminatingEvent args)
    {
        // When entity with comp is deleted, clear references
        var query = EntityQueryEnumerator<InputMoverComponent>();
        while (query.MoveNext(out var mover))
        {
            if (mover.RelativeEntity == ent.Owner)
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
