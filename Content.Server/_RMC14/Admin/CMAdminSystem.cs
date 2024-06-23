using Content.Server.EUI;
using Content.Shared._RMC14.Admin;
using Robust.Shared.Player;

namespace Content.Server._RMC14.Admin;

public sealed class CMAdminSystem : SharedCMAdminSystem
{
    [Dependency] private readonly EuiManager _eui = default!;

    protected override void OpenBui(ICommonSession player, EntityUid target)
    {
        if (!CanUse(player))
            return;

        _eui.OpenEui(new CMAdminEui(target), player);
    }
}
