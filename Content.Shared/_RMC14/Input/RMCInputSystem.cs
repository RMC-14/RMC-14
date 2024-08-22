using Content.Shared._RMC14.CCVar;
using Content.Shared.Movement.Components;
using Robust.Shared.Configuration;
using Robust.Shared.Player;

namespace Content.Shared._RMC14.Input;

public sealed class RMCInputSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _config = default!;

    private bool _activeInputMoverEnabled;

    private EntityQuery<ActorComponent> _actorQuery;

    public override void Initialize()
    {
        _actorQuery = GetEntityQuery<ActorComponent>();

        SubscribeLocalEvent<ActiveInputMoverComponent, MapInitEvent>(OnActiveMapInit);
        SubscribeLocalEvent<ActiveInputMoverComponent, PlayerAttachedEvent>(OnActiveAttached);
        SubscribeLocalEvent<ActiveInputMoverComponent, PlayerDetachedEvent>(OnActiveDetached);

        Subs.CVar(_config, RMCCVars.RMCActiveInputMoverEnabled, v => _activeInputMoverEnabled = v, true);
    }

    private void OnActiveMapInit(Entity<ActiveInputMoverComponent> ent, ref MapInitEvent args)
    {
        if (!_activeInputMoverEnabled)
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
