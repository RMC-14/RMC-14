using Content.Shared._RMC14.GameStates;
using Robust.Server.GameStates;

namespace Content.Server._RMC14.GameStates;

public sealed class RMCPvsSystem : SharedRMCPvsSystem
{
    [Dependency] private readonly PvsOverrideSystem _pvsOverride = default!;

    public override void AddGlobalOverride(EntityUid ent)
    {
        _pvsOverride.AddGlobalOverride(ent);
    }
}
