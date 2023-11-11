using Content.Shared.Actions;
using Content.Shared.Mind;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Shared._CM14.Xenos.Evolution;

public sealed class XenoEvolutionSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _action = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<XenoEvolveActionComponent, MapInitEvent>(OnXenoEvolveActionMapInit);
        SubscribeLocalEvent<XenoComponent, XenoOpenEvolutionsEvent>(OnXenoEvolve);
        SubscribeLocalEvent<XenoComponent, XenoEvolveBuiMessage>(OnXenoEvolveBui);
    }

    private void OnXenoEvolveActionMapInit(Entity<XenoEvolveActionComponent> ent, ref MapInitEvent args)
    {
        if (_action.TryGetActionData(ent, out _, false))
            _action.SetCooldown(ent, _timing.CurTime, _timing.CurTime + ent.Comp.Cooldown);
    }

    private void OnXenoEvolve(Entity<XenoComponent> ent, ref XenoOpenEvolutionsEvent args)
    {
        if (_net.IsClient || !TryComp(ent, out ActorComponent? actor))
            return;

        _ui.TryOpen(ent.Owner, XenoEvolutionUIKey.Key, actor.PlayerSession);
    }

    private void OnXenoEvolveBui(Entity<XenoComponent> ent, ref XenoEvolveBuiMessage args)
    {
        if (!_mind.TryGetMind(ent, out var mindId, out _))
            return;

        var choices = ent.Comp.EvolvesTo.Count;
        if (args.Choice >= choices || args.Choice < 0)
        {
            Log.Warning($"User {args.Session.Name} sent an out of bounds evolution choice: {args.Choice}. Choices: {choices}");
            return;
        }

        var evolution = Spawn(ent.Comp.EvolvesTo[args.Choice], _transform.GetMoverCoordinates(ent.Owner));
        _mind.TransferTo(mindId, evolution);
        _mind.UnVisit(mindId);
        Del(ent.Owner);

        if (TryComp(ent, out ActorComponent? actor))
            _ui.TryClose(ent.Owner, XenoEvolutionUIKey.Key, actor.PlayerSession);
    }
}
