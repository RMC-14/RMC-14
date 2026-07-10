using Content.Shared._RMC14.Marines.Skills;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.DoAfter;
using Content.Shared.Timing;

namespace Content.Shared._RMC14.Medical;

public sealed class RMCHypospraySystem : EntitySystem
{
    [Dependency] private readonly HypospraySystem _hypospray = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly UseDelaySystem _useDelay = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HyposprayComponent, HyposprayDoAfterEvent>(OnHyposprayDoAfter);
    }

    public bool DoAfter(Entity<HyposprayComponent> entity, EntityUid target, EntityUid user)
    {
        if (!_hypospray.EligibleEntity(target, entity))
            return false;

        if (TryComp(entity, out UseDelayComponent? delayComp))
        {
            if (_useDelay.IsDelayed((entity, delayComp)))
                return false;
        }

        var attemptEv = new AttemptHyposprayUseEvent(user, target, TimeSpan.Zero);
        RaiseLocalEvent(entity, ref attemptEv);
        var doAfter = new HyposprayDoAfterEvent();
        var args = new DoAfterArgs(EntityManager, user, attemptEv.DoAfter, doAfter, entity, target, entity)
        {
            BreakOnMove = true,
            BreakOnHandChange = true,
            NeedHand = true
        };

        _doAfter.TryStartDoAfter(args);
        return true;
    }

    private void OnHyposprayDoAfter(Entity<HyposprayComponent> ent, ref HyposprayDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Target is not { } target)
            return;

        args.Handled = true;
        _hypospray.TryDoInject(ent, target, args.User, false);
    }
}
