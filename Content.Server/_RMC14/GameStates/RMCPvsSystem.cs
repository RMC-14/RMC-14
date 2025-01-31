using Content.Shared._RMC14.GameStates;
using Robust.Server.GameStates;
using Robust.Shared.Player;

namespace Content.Server._RMC14.GameStates;

public sealed class RMCPvsSystem : SharedRMCPvsSystem
{
    [Dependency] private readonly PvsOverrideSystem _pvsOverride = default!;

    public override void AddGlobalOverride(EntityUid ent)
    {
        _pvsOverride.AddGlobalOverride(ent);
    }

    public override void AddForceSend(EntityUid ent)
    {
        _pvsOverride.AddForceSend(ent);
    }

    public override void AddSessionOverride(EntityUid ent, ICommonSession session)
    {
        _pvsOverride.AddSessionOverride(ent, session);
    }
}
