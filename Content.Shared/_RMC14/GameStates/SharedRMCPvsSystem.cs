using Robust.Shared.Player;

namespace Content.Shared._RMC14.GameStates;

public abstract class SharedRMCPvsSystem : EntitySystem
{
    public virtual void AddGlobalOverride(EntityUid ent)
    {
    }

    public virtual void AddForceSend(EntityUid ent)
    {
    }

    public virtual void AddSessionOverride(EntityUid ent, ICommonSession session)
    {
    }
}
