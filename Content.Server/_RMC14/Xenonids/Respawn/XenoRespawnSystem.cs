using Content.Server._RMC14.Xenonids.Hive;
using Content.Server.Ghost;
using Content.Server.Ghost.Roles.Components;
using Content.Server.Mind;
using Content.Server.Popups;
using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared._RMC14.Xenonids.Respawn;
using Content.Shared.Mind;
using Content.Shared.Popups;
using Robust.Server.Audio;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server._RMC14.Xenonids.Respawn;

public sealed partial class XenoRespawnSystem : EntitySystem
{
    [Dependency] private readonly GhostSystem _ghostSystem = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly XenoHiveSystem _hive = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    public void RespawnXeno(EntityUid xeno, TimeSpan time, bool atCorpse = false, EntityCoordinates? corpse = null)
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

            ActorComponent? actor;
            if (respawn.RespawnAtCorpse)
            {
                if (respawn.CorpseLocation == null)
                {
                    RemCompDeferred<XenoRespawnComponent>(ghost);
                    continue;
                }

                var spawn = SpawnAtPosition(respawn.Larva, respawn.CorpseLocation.Value);
                _hive.SetHive(spawn, respawn.Hive);

                if (!TryComp(ghost, out actor))
                    continue;

                var session = actor.PlayerSession;

                if (!_mind.TryGetMind(session, out var mindId, out var mindComp))
                    continue;

                _mind.TransferTo(mindId, spawn);

                _popup.PopupEntity(Loc.GetString("rmc-xeno-respawn-corpse-self"), spawn, spawn, PopupType.MediumCaution);
                _popup.PopupEntity(Loc.GetString("rmc-xeno-respawn-corpse-others"), spawn, Filter.PvsExcept(spawn), true, PopupType.MediumCaution);

                _audio.PlayPvs(respawn.CorpseSound, _transform.GetMoverCoordinates(spawn));

                RemCompDeferred<XenoRespawnComponent>(ghost); // If this fails, somehow
                continue;
            }

            if (respawn.Hive == null || !TryComp<HiveComponent>(respawn.Hive, out var hiveComp))
                continue;


            if (TryComp(ghost, out actor))
            {
                _hive.IncreaseBurrowedLarva((respawn.Hive.Value, hiveComp), 1);
                _hive.JoinBurrowedLarva((respawn.Hive.Value, hiveComp), actor.PlayerSession);
            }

            _popup.PopupEntity(Loc.GetString("rmc-xeno-respawn-fail"), ghost, ghost, PopupType.Large);
            RemCompDeferred<XenoRespawnComponent>(ghost);
        }
    }
}
