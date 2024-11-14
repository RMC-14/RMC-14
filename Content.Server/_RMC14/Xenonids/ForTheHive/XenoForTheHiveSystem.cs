using Content.Server.Ghost.Roles.Components;
using Content.Server.Ghost;
using Content.Server.Mind;
using Content.Shared._RMC14.Xenonids.ForTheHive;
using Content.Shared.Mind;
using Robust.Shared.Player;
using Content.Shared._RMC14.Xenonids.Hive;
using Content.Server._RMC14.Spawners;
using Robust.Shared.Map;
using Content.Server.Chat.Systems;

namespace Content.Server._RMC14.Xenonids.ForTheHive;

public sealed class XenoForTheHiveSystem : SharedXenoForTheHiveSystem
{
    [Dependency] private readonly GhostSystem _ghostSystem = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly ChatSystem _chat = default!;

    protected override void ForTheHiveShout(EntityUid xeno)
    {
        _chat.TrySendInGameICMessage(xeno, Loc.GetString("rmc-xeno-for-the-hive-announce"), InGameICChatType.Speak, false);
    }

    protected override void ForTheHiveRespawn(EntityUid xeno, TimeSpan time, bool atCorpse = false, EntityCoordinates? corpse = null)
    {
        if (!TryComp(xeno, out ActorComponent? actor))
            return;

        RemComp<GhostTakeoverAvailableComponent>(xeno);

        var session = actor.PlayerSession;

        Entity<MindComponent> mind;

        if (_mind.TryGetMind(session, out var mindId, out var mindComp))
            mind = (mindId, mindComp);
        else
            mind = _mind.CreateMind(session.UserId);

        var ghost = _ghostSystem.SpawnGhost((mind.Owner, mind.Comp), xeno);

        if (ghost != null)
        {
            var respawn = EnsureComp<XenoRespawnComponent>(ghost.Value);
            respawn.Hive = _hive.GetHive(xeno);
            respawn.RespawnAt = _timing.CurTime + time;
            respawn.RespawnAtCorpse = atCorpse;
            respawn.CorpseLocation = corpse;
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var time = _timing.CurTime;

        var respawnQuery = EntityQueryEnumerator<XenoRespawnComponent>();

        while (respawnQuery.MoveNext(out var ghost, out var respawn))
        {
            if (time < respawn.RespawnAt)
                continue;

            if (respawn.RespawnAtCorpse)
            {
                if (respawn.CorpseLocation == null)
                {
                    RemCompDeferred<XenoRespawnComponent>(ghost);
                    continue;
                }

                var spawn = SpawnAtPosition(respawn.Larva, respawn.CorpseLocation.Value);
                _hive.SetHive(spawn, respawn.Hive);

                if (!TryComp(ghost, out ActorComponent? actor))
                    continue;

                var session = actor.PlayerSession;

                if (!_mind.TryGetMind(session, out var mindId, out var mindComp))
                    continue;

                _mind.TransferTo(mindId, spawn);

                _popup.PopupEntity(Loc.GetString("rmc-xeno-for-the-hive-respawn-corpse-self"), spawn, spawn, Shared.Popups.PopupType.MediumCaution);
                _popup.PopupEntity(Loc.GetString("rmc-xeno-for-the-hive-respawn-corpse-others"), spawn, Filter.PvsExcept(spawn), true, Shared.Popups.PopupType.MediumCaution);

                _audio.PlayPvs(respawn.CorpseSound, _transform.GetMoverCoordinates(spawn));

                RemCompDeferred<XenoRespawnComponent>(ghost); // If this fails, somehow
                continue;
            }

            if (respawn.Hive == null || !TryComp<HiveComponent>(respawn.Hive, out var hiveComp))
                continue;

            _hive.IncreaseBurrowedLarva((respawn.Hive.Value, hiveComp), 1);
            _hive.JoinBurrowedLarva((respawn.Hive.Value, hiveComp), ghost);

            _popup.PopupEntity(Loc.GetString("rmc-xeno-for-the-hive-respawn-fail"), ghost, ghost, Shared.Popups.PopupType.Large);
            RemCompDeferred<XenoRespawnComponent>(ghost);
        }
    }
}
