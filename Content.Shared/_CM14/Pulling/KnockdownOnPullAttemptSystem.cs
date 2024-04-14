using Content.Shared.IdentityManagement;
using Content.Shared.Movement.Pulling.Events;
using Content.Shared.Popups;
using Content.Shared.Stunnable;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Shared._CM14.Pulling;

public sealed class KnockdownOnPullAttemptSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<KnockdownOnPullAttemptComponent, PullAttemptEvent>(OnKnockdownOnPullAttempt);
    }

    private void OnKnockdownOnPullAttempt(Entity<KnockdownOnPullAttemptComponent> ent, ref PullAttemptEvent args)
    {
        if (args.PulledUid != ent.Owner ||
            HasComp<KnockdownOnPullAttemptImmuneComponent>(args.PullerUid))
        {
            return;
        }

        _stun.TryStun(args.PullerUid, ent.Comp.Duration, true);
        _stun.TryKnockdown(args.PullerUid, ent.Comp.Duration, true);
        args.Cancelled = true;

        if (!_timing.IsFirstTimePredicted)
            return;

        foreach (var session in Filter.Pvs(args.PullerUid).Recipients)
        {
            if (session == IoCManager.Resolve<ISharedPlayerManager>().LocalSession)
                continue;

            var puller = Identity.Name(args.PullerUid, EntityManager, session.AttachedEntity);
            var pulled = Identity.Name(ent, EntityManager, session.AttachedEntity);
            var message = $"{puller} tried to pull {pulled} but instead gets a tail swipe to the head!";
            _popup.PopupEntity(message, args.PullerUid, session, PopupType.MediumCaution);
        }
    }
}
