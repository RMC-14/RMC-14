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
    [Dependency] private readonly XenoPlasmaSystem _xenoPlasma = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<XenoEvolveActionComponent, MapInitEvent>(OnXenoEvolveActionMapInit);
        SubscribeLocalEvent<XenoComponent, XenoOpenEvolutionsActionEvent>(OnXenoEvolveAction);
        SubscribeLocalEvent<XenoComponent, XenoEvolveBuiMessage>(OnXenoEvolveBui);
        SubscribeLocalEvent<XenoComponent, XenoEvolutionDoAfterEvent>(OnXenoEvolveDoAfter);
    }

    private void OnXenoEvolveActionMapInit(Entity<XenoEvolveActionComponent> ent, ref MapInitEvent args)
    {
        if (_action.TryGetActionData(ent, out _, false))
            _action.SetCooldown(ent, _timing.CurTime, _timing.CurTime + ent.Comp.Cooldown);
    }

    private void OnXenoEvolveAction(Entity<XenoComponent> xeno, ref XenoOpenEvolutionsActionEvent args)
    {
        if (_net.IsClient || !TryComp(xeno, out ActorComponent? actor))
            return;

        _ui.TryOpen(xeno.Owner, XenoEvolutionUIKey.Key, actor.PlayerSession);
    }

    private void OnXenoEvolveBui(Entity<XenoComponent> xeno, ref XenoEvolveBuiMessage args)
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

    private void OnXenoEvolveDoAfter(Entity<XenoComponent> xeno, ref XenoEvolutionDoAfterEvent args)
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

        var oldRotation = _transform.GetWorldRotation(xeno);
        var oldPlasma = xeno.Comp.Plasma;
        var coordinates = _transform.GetMoverCoordinates(xeno.Owner);
        var newXeno = Spawn(args.Choice, coordinates);
        _mind.TransferTo(mindId, newXeno);
        _mind.UnVisit(mindId);
        Del(xeno.Owner);

        if (TryComp(newXeno, out XenoComponent? newXenoComp))
        {
            var newPlasma = FixedPoint2.Min(oldPlasma, newXenoComp.MaxPlasma);
            _xenoPlasma.SetPlasma((newXeno, newXenoComp), newPlasma);
        }

        _transform.SetWorldRotation(newXeno, oldRotation);
        _popup.PopupEntity(Loc.GetString("cm-xeno-evolution-end"), newXeno, newXeno);
    }
}
