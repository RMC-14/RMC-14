using Content.Server.Wires;
using Content.Shared._RMC14.Power;

namespace Content.Server._RMC14.Power;

public sealed partial class RMCPowerSystem
{
    protected override void SetApcWiresCut(Entity<RMCApcComponent> ent, EntityUid user, bool cut)
    {
        if (TryComp(ent, out WiresComponent? wires))
            _wires.SetWiresCut((ent, wires), user, cut);
    }
}
