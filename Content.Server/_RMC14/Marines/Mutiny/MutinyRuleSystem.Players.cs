using System.Linq;
using Content.Shared.GameTicking;
using Content.Shared.Mind.Components;
using Robust.Shared.Player;

namespace Content.Server._RMC14.Marines.Mutiny;

public sealed partial class MutinyRuleSystem
{
    private void OnPlayerSpawnComplete(PlayerSpawnCompleteEvent args)
    {
        HandleBodyAvailable(args.Mob);
    }

    private void OnPlayerAttached(PlayerAttachedEvent args)
    {
        HandleBodyAvailable(args.Entity);
    }

    private void OnPlayerDetached(PlayerDetachedEvent args)
    {
        if (!_mind.TryGetMind(args.Entity, out var mindId, out _))
            return;

        if (_pendingSideChoices.TryGetValue(mindId, out var choice))
            choice.ResolveDefault();
        if (_pendingInvites.TryGetValue(mindId, out var invite))
            invite.Cancel();
        if (_pendingBegins.TryGetValue(mindId, out var begin))
            begin.Cancel();

        foreach (var outboundInvite in _pendingInvites.Values
                     .Where(invite => invite.LeaderMind == mindId)
                     .ToArray())
        {
            outboundInvite.Cancel();
        }
    }

    private void OnMindAdded(Entity<MindContainerComponent> body, ref MindAddedMessage args)
    {
        HandleBodyAvailable(body.Owner);
    }

    private void OnMindRemoved(Entity<MindContainerComponent> body, ref MindRemovedMessage args)
    {
        if (TryComp(args.Mind.Owner, out MutinyMindComponent? mutinyMind))
            RemoveProjection(body.Owner, mutinyMind.Rule);
    }

    private void HandleBodyAvailable(EntityUid body)
    {
        if (!TryGetActiveMutiny(out var rule) ||
            !_mind.TryGetMind(body, out var mindId, out var mind))
        {
            return;
        }

        if (TryComp(mindId, out MutinyMindComponent? mutinyMind) && mutinyMind.Rule == rule.Owner)
            ProjectMindToBody((mindId, mutinyMind, mind), rule);

        if (rule.Comp.Phase == Content.Shared._RMC14.Marines.Mutiny.MutinyPhase.Active)
            ClassifyBody(body, rule);
    }
}
