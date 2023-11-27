using Content.Shared._CM14.Xenos.Plasma;
using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.Mind;
using Content.Shared.Popups;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Shared._CM14.Xenos.Evolution;

public sealed class XenoEvolutionSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _action = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<XenoEvolveActionComponent, MapInitEvent>(OnXenoEvolveActionMapInit);
        SubscribeLocalEvent<XenoEvolutionComponent, XenoOpenEvolutionsActionEvent>(OnXenoEvolveAction);
        SubscribeLocalEvent<XenoEvolutionComponent, XenoEvolveBuiMessage>(OnXenoEvolveBui);
        SubscribeLocalEvent<XenoEvolutionComponent, XenoEvolutionDoAfterEvent>(OnXenoEvolveDoAfter);
    }

    private void OnXenoEvolveActionMapInit(Entity<XenoEvolveActionComponent> ent, ref MapInitEvent args)
    {
        if (_action.TryGetActionData(ent, out _, false))
            _action.SetCooldown(ent, _timing.CurTime, _timing.CurTime + ent.Comp.Cooldown);
    }

    private void OnXenoEvolveAction(Entity<XenoEvolutionComponent> xeno, ref XenoOpenEvolutionsActionEvent args)
    {
        if (_net.IsClient || !TryComp(xeno, out ActorComponent? actor))
            return;

        _ui.TryOpen(xeno.Owner, XenoEvolutionUIKey.Key, actor.PlayerSession);
    }

    private void OnXenoEvolveBui(Entity<XenoEvolutionComponent> xeno, ref XenoEvolveBuiMessage args)
    {
        if (!xeno.Comp.EvolvesTo.Contains(args.Choice))
        {
            Log.Warning($"User {args.Session.Name} sent an invalid evolution choice: {args.Choice}.");
            return;
        }

        var ev = new XenoEvolutionDoAfterEvent(args.Choice);
        var doAfter = new DoAfterArgs(EntityManager, xeno, xeno.Comp.EvolutionDelay, ev, xeno);

        if (xeno.Comp.EvolutionDelay > TimeSpan.Zero)
        {
            _popup.PopupEntity(Loc.GetString("cm-xeno-evolution-start"), xeno, xeno);
        }

        _doAfter.TryStartDoAfter(doAfter);

        if (_net.IsClient)
            return;

        if (TryComp(xeno, out ActorComponent? actor))
            _ui.TryClose(xeno.Owner, XenoEvolutionUIKey.Key, actor.PlayerSession);
    }

    private void OnXenoEvolveDoAfter(Entity<XenoEvolutionComponent> xeno, ref XenoEvolutionDoAfterEvent args)
    {
        if (_net.IsClient ||
            args.Handled ||
            args.Cancelled ||
            !_mind.TryGetMind(xeno, out var mindId, out _) ||
            !xeno.Comp.EvolvesTo.Contains(args.Choice))
        {
            return;
        }

        args.Handled = true;

        var coordinates = _transform.GetMoverCoordinates(xeno.Owner);
        var newXeno = Spawn(args.Choice, coordinates);
        _mind.TransferTo(mindId, newXeno);
        _mind.UnVisit(mindId);

        var ev = new NewXenoEvolvedComponent(xeno);
        RaiseLocalEvent(newXeno, ref ev);

        Del(xeno.Owner);

        _popup.PopupEntity(Loc.GetString("cm-xeno-evolution-end"), newXeno, newXeno);
    }
}
