using Content.Shared.IdentityManagement;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Pulling.Events;
using Content.Shared.Popups;
using Content.Shared.Stunnable;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Shared._CM14.Pulling;

public sealed class ParalyzeOnPullAttemptSystem : EntitySystem
{
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ParalyzeOnPullAttemptComponent, PullAttemptEvent>(OnParalyzeOnPullAttempt);
    }

    private void OnParalyzeOnPullAttempt(Entity<ParalyzeOnPullAttemptComponent> ent, ref PullAttemptEvent args)
    {
        var user = args.PullerUid;
        var target = args.PulledUid;
        if (target != ent.Owner ||
            HasComp<ParalyzeOnPullAttemptImmuneComponent>(user) ||
            _mobState.IsIncapacitated(ent))
        {
            return;
        }

        _stun.TryParalyze(user, ent.Comp.Duration, true);
        args.Cancelled = true;

        if (!_timing.IsFirstTimePredicted)
            return;

        foreach (var session in Filter.Pvs(user).Recipients)
        {
            if (session == IoCManager.Resolve<ISharedPlayerManager>().LocalSession)
                continue;

            var puller = Identity.Name(user, EntityManager, session.AttachedEntity);
            var pulled = Identity.Name(ent, EntityManager, session.AttachedEntity);
            var message = $"{puller} tried to pull {pulled} but instead gets a tail swipe to the head!";
            _popup.PopupEntity(message, user, session, PopupType.MediumCaution);
        }
    }
}
