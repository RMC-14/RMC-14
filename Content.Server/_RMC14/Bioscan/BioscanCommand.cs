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
        TimeSpan lastMarine = default;
        int maxXenoAlive = default;
        var sound = new BioscanComponent().MarineSound;
        _bioscan.TryBioscanARES(ref lastMarine, ref maxXenoAlive, sound, true);
    }

    [CommandImplementation("xeno")]
    public void Xeno()
    {
        _bioscan ??= GetSys<BioscanSystem>();
        TimeSpan lastXeno = default;
        int maxMarineAlive = default;
        var sound = new BioscanComponent().XenoSound;
        _bioscan.TryBioscanQueenMother(ref lastXeno, ref maxMarineAlive, sound, true);
    }
}
