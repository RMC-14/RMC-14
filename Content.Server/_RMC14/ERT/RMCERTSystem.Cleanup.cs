using System.Linq;
using System.Numerics;
using System.Text;
using Content.Server._RMC14.Dropship;
using Content.Server._RMC14.Rules.DistressSignal;
using Content.Server._RMC14.Marines;
using Content.Server.Administration;
using Content.Server.Administration.Logs;
using Content.Server.Administration.Managers;
using Content.Server.Chat.Managers;
using Content.Server.GameTicking;
using Content.Server.Ghost;
using Content.Server.Ghost.Roles.Components;
using Content.Server.Humanoid.Components;
using Content.Server.Humanoid.Systems;
using Content.Server.Mind;
using Content.Server.Popups;
using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Events;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared._RMC14.AlertLevel;
using Content.Shared._RMC14.Areas;
using Content.Shared._RMC14.CCVar;
using Content.Shared._RMC14.Dialog;
using Content.Shared._RMC14.Dropship;
using Content.Shared._RMC14.ERT;
using Content.Shared._RMC14.Evacuation;
using Content.Shared._RMC14.Marines.Announce;
using Content.Shared._RMC14.Rules;
using Content.Shared.Buckle;
using Content.Shared.Database;
using Content.Shared.Doors.Components;
using Content.Shared.Ghost.Roles.Components;
using Content.Shared.GameTicking;
using Content.Shared.Interaction.Events;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Content.Shared.Prototypes;
using Content.Shared.Shuttles.Components;
using Content.Shared.Shuttles.Systems;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server._RMC14.ERT;

public sealed partial class RMCERTSystem
{

    private bool HasActiveRecruitmentRaffles(RMCERTRequest request)
    {
        foreach (var slot in request.SpawnedGhostRoles)
        {
            if (Exists(slot) && HasComp<GhostRoleRaffleComponent>(slot))
                return true;
        }

        return false;
    }

    private int FinalizeRecruitment(RMCERTRequest request)
    {
        var accepted = 0;
        // Trim unclaimed ghost roles before launch so the minimum-slot check only counts accepted responders.
        for (var i = request.SpawnedGhostRoles.Count - 1; i >= 0; i--)
        {
            var member = request.SpawnedGhostRoles[i];
            if (!Exists(member))
            {
                request.SpawnedGhostRoles.RemoveAt(i);
                continue;
            }

            if (TryComp(member, out GhostRoleComponent? ghostRole) &&
                !ghostRole.Taken)
            {
                QueueDel(member);
                request.SpawnedGhostRoles.RemoveAt(i);
                continue;
            }

            accepted++;
        }

        return accepted;
    }

    private void CleanupRequestContent(RMCERTRequest request, string reason)
    {
        // Failed and cancelled requests need to unwind both the staged roster and any destination reservation held by the shuttle.
        var ghostCoordinates = _gameTicker.GetObserverSpawnPoint();
        var ghostedMembers = 0;
        foreach (var member in request.SpawnedGhostRoles)
        {
            if (!Exists(member))
                continue;

            // Cleanup can move a player to a ghost and delete their old body in the same tick.
            // Close their UIs first so clients don't keep stale BUIs for entities queued for deletion.
            _ui.CloseUserUis(member);

            if (_mind.TryGetMind(member, out var mindId, out var mind))
            {
                if (TryGhostMindForCleanup(request, member, mindId, mind, ghostCoordinates, reason))
                    ghostedMembers++;
            }

            QueueDel(member);
        }

        request.SpawnedGhostRoles.Clear();
        request.PlannedRoster.Clear();

        if (request.Shuttle is { Valid: true } shuttle && Exists(shuttle))
        {
            ReleasePrelaunchShuttleDoorLocks(shuttle);
            ClearShuttlePlayerRouteLock(shuttle);
            RemComp<RMCERTShuttleComponent>(shuttle);
            RemComp<RMCRestrictedShuttleComponent>(shuttle);
            RemComp<RMCExpectedDockComponent>(shuttle);

            if (TryComp(shuttle, out DropshipComponent? dropship) &&
                dropship.Destination is { } destination &&
                TryComp(destination, out DropshipDestinationComponent? destinationComp) &&
                destinationComp.Ship == shuttle)
            {
                _dropship.SetDestinationShip((destination, destinationComp), null);
            }

            var actorsGhosted = TryGhostActorsOffShuttle(request, shuttle, ghostCoordinates, reason, out var actorsOnShuttle, out var ghostedActors);
            var remainingActors = CountActorsOnShuttle(shuttle);
            var unhandledActors = Math.Max(0, actorsOnShuttle - ghostedActors);
            Log.Info($"ERT request {request.Id} cleanup '{reason}' for {ToPrettyString(shuttle)}. " +
                      $"GhostedMembers={ghostedMembers}, ActorsOnShuttle={actorsOnShuttle}, " +
                      $"GhostedActors={ghostedActors}, UnhandledActors={unhandledActors}, RemainingActors={remainingActors}, " +
                      GetShuttleDiagnostics(request, shuttle));

            if (actorsGhosted && unhandledActors == 0)
            {
                QueueDel(shuttle);
                request.Shuttle = null;
                request.ShuttleSpawnMarker = null;
                _chat.SendAdminAnnouncement(Loc.GetString("rmc-ert-admin-cleanup",
                    ("id", request.Id),
                    ("reason", reason)));
            }
            else
            {
                Log.Warning($"ERT request {request.Id} left shuttle {ToPrettyString(shuttle)} in world after cleanup '{reason}' " +
                            $"because {remainingActors} actor(s) could not be ghosted safely.");
                _chat.SendAdminAnnouncement(Loc.GetString("rmc-ert-admin-cleanup-deferred",
                    ("id", request.Id),
                    ("reason", reason),
                    ("actors", remainingActors)));
            }
        }
        else
        {
            request.Shuttle = null;
            request.ShuttleSpawnMarker = null;
        }

        DeleteReturnDestination(request);
    }

    private bool TryGhostActorsOffShuttle(
        RMCERTRequest request,
        EntityUid shuttle,
        EntityCoordinates ghostCoordinates,
        string reason,
        out int actorsOnShuttle,
        out int ghostedActors)
    {
        var actors = new List<EntityUid>();
        var query = EntityQueryEnumerator<ActorComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out _, out var xform))
        {
            if (TerminatingOrDeleted(uid))
                continue;

            if (xform.GridUid != shuttle)
                continue;

            actors.Add(uid);
        }

        actorsOnShuttle = actors.Count;
        ghostedActors = 0;

        if (actors.Count == 0)
            return true;

        foreach (var actor in actors)
        {
            if (!Exists(actor) || TerminatingOrDeleted(actor))
            {
                ghostedActors++;
                continue;
            }

            // Close all UIs before the mind is moved away and this shuttle actor is queued for deletion.
            _ui.CloseUserUis(actor);

            if (!_mind.TryGetMind(actor, out var mindId, out var mind) &&
                (!TryComp(actor, out ActorComponent? actorComp) ||
                 !_mind.TryGetMind(actorComp.PlayerSession, out mindId, out mind)))
            {
                Log.Warning($"ERT request {request.Id} cleanup '{reason}' found actor {ToPrettyString(actor)} " +
                            $"on shuttle {ToPrettyString(shuttle)} but could not find a mind to ghost.");
                continue;
            }

            if (!TryGhostMindForCleanup(request, actor, mindId, mind, ghostCoordinates, reason))
                continue;

            QueueDel(actor);
            ghostedActors++;
        }

        // QueueDel(actor) is processed later, so an immediate entity query can still see
        // actors that were already safely ghosted and queued for deletion.
        return ghostedActors >= actorsOnShuttle;
    }

    private bool TryGhostMindForCleanup(
        RMCERTRequest request,
        EntityUid actor,
        EntityUid mindId,
        MindComponent mind,
        EntityCoordinates ghostCoordinates,
        string reason)
    {
        var ghost = _ghost.SpawnGhost((mindId, mind), ghostCoordinates, canReturn: false);
        if (ghost == null)
        {
            Log.Warning($"ERT request {request.Id} cleanup '{reason}' failed to ghost " +
                        $"{ToPrettyString(actor)} from cleanup shuttle.");
            return false;
        }

        Log.Info($"ERT request {request.Id} cleanup '{reason}' ghosted {ToPrettyString(actor)} " +
                 $"as {ToPrettyString(ghost.Value)}.");
        return true;
    }

    private int CountActorsOnShuttle(EntityUid shuttle)
    {
        var count = 0;
        var query = EntityQueryEnumerator<ActorComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out _, out var xform))
        {
            if (!TerminatingOrDeleted(uid) && xform.GridUid == shuttle)
                count++;
        }

        return count;
    }
}
