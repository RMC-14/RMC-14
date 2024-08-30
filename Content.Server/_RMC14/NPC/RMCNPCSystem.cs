using Content.Server.NPC.Systems;
using Content.Shared._RMC14.NPC;

namespace Content.Server._RMC14.NPC;

public sealed class RMCNPCSystem : SharedRMCNPCSystem
{
    [Dependency] private readonly NPCSystem _npc = default!;

    public override void SleepNPC(EntityUid id)
    {
        base.SleepNPC(id);
        _npc.SleepNPC(id);
    }

    public override void WakeNPC(EntityUid id)
    {
        base.WakeNPC(id);
        _npc.WakeNPC(id);
    }
}
