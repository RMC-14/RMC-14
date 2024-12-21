using Content.Server._RMC14.Xenonids.Respawn;
using Content.Server.Chat.Systems;
using Content.Shared._RMC14.Xenonids.Heal;
using Robust.Shared.Map;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Server._RMC14.Xenonids.Heal;

public sealed partial class XenoHealSystem : SharedXenoHealSystem
{
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly XenoRespawnSystem _xenoRespawn = default!;

    protected override void SacraficialHealShout(EntityUid xeno)
    {
        _chat.TrySendInGameICMessage(xeno, Loc.GetString("rmc-xeno-for-the-hive-announce"), InGameICChatType.Speak, false);
    }

    protected override void SacraficialHealRespawn(EntityUid xeno, TimeSpan time, bool atCorpse = false, EntityCoordinates? corpse = null)
    {
        _xenoRespawn.RespawnXeno(xeno, time, atCorpse, corpse);
    }
}
