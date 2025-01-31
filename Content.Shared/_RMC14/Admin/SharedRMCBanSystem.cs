using Content.Shared.Roles;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Admin;

public abstract class SharedRMCBanSystem : EntitySystem
{
    public bool IsJobBanned(NetUserId user, ProtoId<JobPrototype> job)
    {
        return false;
    }

    public bool IsJobBanned(Entity<ActorComponent?> user, ProtoId<JobPrototype> job)
    {
        return Resolve(user, ref user.Comp, false) && IsJobBanned(user.Comp.PlayerSession.UserId, job);
    }
}
