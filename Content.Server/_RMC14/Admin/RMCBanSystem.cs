using Content.Server.Administration.Managers;
using Content.Shared._RMC14.Admin;
using Content.Shared.Roles;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Server._RMC14.Admin;

public sealed class RMCBanSystem : SharedRMCBanSystem
{
    [Dependency] private readonly IBanManager _ban = default!;

    public bool IsJobBanned(NetUserId user, ProtoId<JobPrototype> job)
    {
        return _ban.GetJobBans(user)?.Contains(job) ?? false;
    }
}
