using Content.Server.Administration;
using Content.Shared._RMC14.Bioscan;
using Content.Shared.Administration;
using Robust.Shared.Toolshed;

namespace Content.Server._RMC14.Bioscan;

[ToolshedCommand, AdminCommand(AdminFlags.Moderator)]
public sealed class BioscanCommand : ToolshedCommand
{
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
            _bioscan.TryBioscanARES((uid, bioscan), true);
        }
    }

    [CommandImplementation("xeno")]
    public void Xeno()
    {
        _bioscan ??= GetSys<BioscanSystem>();

        var bioscans = EntityManager.EntityQueryEnumerator<BioscanComponent>();
        while (bioscans.MoveNext(out var uid, out var bioscan))
        {
            _bioscan.TryBioscanQueenMother((uid, bioscan), true);
        }
    }
}
