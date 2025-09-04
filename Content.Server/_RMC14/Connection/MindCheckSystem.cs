using Content.Server.Mind;
using Content.Shared._RMC14.Connection;
using Content.Shared.Ghost;
using Content.Shared.Mind.Components;
using Robust.Server.Player;
using Robust.Shared.Enums;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server._RMC14.Connection;

public sealed class MindCheckSystem : EntitySystem
{
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    public override void Initialize()
    {
        SubscribeLocalEvent<MindCheckComponent, MindAddedMessage>(OnMindChanged);
        SubscribeLocalEvent<MindCheckComponent, MindRemovedMessage>(OnMindChanged);

        SubscribeLocalEvent<MindCheckComponent, MindUnvisitedMessage>(OnMindStateChange);
        SubscribeLocalEvent<MindCheckComponent, PlayerAttachedEvent>(OnMindStateChange);
        SubscribeLocalEvent<MindCheckComponent, PlayerDetachedEvent>(OnMindStateChange);

        _player.PlayerStatusChanged += PlayerStatusChanged;
    }

    private void OnMindChanged<T>(Entity<MindCheckComponent> ent, ref T args) where T : MindEvent
    {
        CheckMind(ent);
    }

    private void OnMindStateChange<T>(Entity<MindCheckComponent> ent, ref T args) where T : EntityEventArgs
    {
        CheckMind(ent);
    }

    private void PlayerStatusChanged(object? sender, SessionStatusEventArgs args)
    {
        if (args.Session.AttachedEntity is not { } entity)
            return;

        GhostComponent? ghost = null;

        if (!TryComp<MindCheckComponent>(entity, out var mind) && !TryComp(entity, out ghost))
            return;

        if (ghost != null)
        {
            if (!_mind.TryGetMind(entity, out var mindId, out var mindComp) || mindComp.OwnedEntity == null)
                return;

            if (!TryComp<MindCheckComponent>(mindComp.OwnedEntity, out var check))
                return;

            CheckMind((mindComp.OwnedEntity.Value, check));
        }
        else if (mind != null)
            CheckMind((entity, mind));
    }

    public override void Update(float frameTime)
    {
        var time = _timing.CurTime;

        var victimQuery = EntityQueryEnumerator<MindCheckComponent>();
        while (victimQuery.MoveNext(out var uid, out var mind))
        {
            if (mind.NextCheck > time)
                continue;

            CheckMind((uid, mind));
        }
    }

    private void CheckMind(Entity<MindCheckComponent> mind)
    {
        if (!_mind.TryGetMind(mind.Owner, out _, out var mindComp) ||
            mindComp.UserId == null ||
            !_player.TryGetSessionById(mindComp.UserId, out var session) ||
            session.Status == SessionStatus.Disconnected)
        {
            mind.Comp.ActiveMindOrGhost = false;
        }
        else
        {
            mind.Comp.ActiveMindOrGhost = true;
        }

        mind.Comp.NextCheck = _timing.CurTime + mind.Comp.CheckEvery;

        Dirty(mind);
    }
}
