using Content.Server._RMC14.Xenonids.Respawn;
using Content.Server.Chat.Systems;
using Content.Server.Ghost;
using Content.Server.Mind;
using Content.Shared._RMC14.Xenonids.Heal;
using Content.Shared.Mind;
using Robust.Shared.Map;
using Robust.Shared.Player;

namespace Content.Server._RMC14.Xenonids.Heal;

public sealed partial class XenoHealSystem : SharedXenoHealSystem
{
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly XenoRespawnSystem _xenoRespawn = default!;
    [Dependency] private readonly GhostSystem _ghost = default!;
    [Dependency] private readonly MindSystem _mind = default!;

    protected override void SacrificialHealShout(EntityUid xeno)
    {
        _chat.TrySendInGameICMessage(xeno, Loc.GetString("rmc-xeno-sacrifice-heal-announce"), InGameICChatType.Speak, false);
    }

    protected override void SacrificialHealRespawn(EntityUid xeno, TimeSpan time, bool atCorpse = false, EntityCoordinates? corpse = null)
    {
        _xenoRespawn.RespawnXeno(xeno, time, atCorpse, corpse);
    }

    protected override void SacrificeNoRespawn(EntityUid xeno)
    {
        if (!TryComp(xeno, out ActorComponent? actor))
            return;

        var session = actor.PlayerSession;

        Entity<MindComponent> mind;
        if (_mind.TryGetMind(session, out var mindId, out var mindComp))
            mind = (mindId, mindComp);
        else
            mind = _mind.CreateMind(session.UserId);

        _ghost.SpawnGhost((mind.Owner, mind.Comp), xeno);
    }
}
