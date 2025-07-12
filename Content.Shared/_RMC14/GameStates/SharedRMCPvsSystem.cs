using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Shared._RMC14.GameStates;

public abstract class SharedRMCPvsSystem : EntitySystem
{
    [Dependency] private readonly ISharedPlayerManager _player = default!;

    public virtual void AddGlobalOverride(EntityUid ent)
    {
    }

    public virtual void RemoveGlobalOverride(EntityUid ent)
    {
    }

    public virtual void AddForceSend(EntityUid ent)
    {
    }

    public virtual void AddSessionOverride(EntityUid ent, ICommonSession session)
    {
    }

    public virtual void AddSessionOverride(EntityUid ent, NetUserId sessionId)
    {
        if (_player.TryGetSessionById(sessionId, out var session))
            AddSessionOverride(ent, session);
    }
}
