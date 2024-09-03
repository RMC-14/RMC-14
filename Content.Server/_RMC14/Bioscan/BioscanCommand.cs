using Content.Server.Administration;
using Content.Shared._RMC14.Bioscan;
using Content.Shared.Administration;
using Robust.Shared.Timing;
using Robust.Shared.Toolshed;

namespace Content.Server._RMC14.Bioscan;

[ToolshedCommand, AdminCommand(AdminFlags.Moderator)]
public sealed class BioscanCommand : ToolshedCommand
{
    [Dependency] private readonly IGameTiming _timing = default!;

    private BioscanSystem? _bioscan;

    [CommandImplementation("all")]
    public void All()
    {
        Marine();
        Xeno();
    }

    [CommandImplementation("marine")]
    public void Marine()
    {
        _bioscan ??= GetSys<BioscanSystem>();

        var bioscans = EntityManager.EntityQueryEnumerator<BioscanComponent>();
        while (bioscans.MoveNext(out var uid, out var bioscan))
        {
            if (_bioscan.TryBioscanARES(ref bioscan.LastMarine, ref bioscan.MaxXenoAlive, bioscan.MarineSound, true))
            {
                EntityManager.Dirty(uid, bioscan);
                return;
            }
        }
    }

    [CommandImplementation("xeno")]
    public void Xeno()
    {
        _bioscan ??= GetSys<BioscanSystem>();

        var bioscans = EntityManager.EntityQueryEnumerator<BioscanComponent>();
        while (bioscans.MoveNext(out var uid, out var bioscan))
        {
            if (_bioscan.TryBioscanARES(ref bioscan.LastXeno, ref bioscan.MaxMarinesAlive, bioscan.XenoSound, true))
            {
                EntityManager.Dirty(uid, bioscan);
                return;
            }
        }
    }
}
