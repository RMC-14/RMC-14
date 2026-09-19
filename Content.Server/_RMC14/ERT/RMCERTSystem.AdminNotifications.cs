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

    /// <summary>
    /// Builds a flattened snapshot for the admin ERT window.
    /// This is UI state only: callers receive safe DTOs instead of mutable server-side <see cref="RMCERTRequest"/> instances.
    /// </summary>
    /// <param name="canForceCalls">Whether the viewing admin may see and use force-call controls.</param>
    /// <returns>Current request/call state for the admin EUI.</returns>
    public RMCERTAdminEuiState CreateAdminState(bool canForceCalls)
    {
        // The admin window works off a flattened snapshot so the client does not need to know about runtime-only request objects.
        var requests = _requests.Values
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new RMCERTRequestOption(
                r.Id,
                r.State,
                r.Source,
                r.SourceName,
                r.RequesterName,
                r.Reason,
                GetSelectedCallLabel(r),
                r.AllowedCalls.Select(c => c.Id).ToList(),
                r.AllowRandomSelection,
                r.AllowSpecificSelection,
                FormatRoundTime(r.CreatedAt),
                r.LastError,
                r.LastWarning))
            .ToList();

        var calls = GetCallOptions(new RMCERTCallQueryArgs());

        var forceCalls = canForceCalls
            ? GetCallOptions(new RMCERTCallQueryArgs
            {
                EnabledOnly = true,
                AdminSelectableOnly = true,
            })
            : [];

        return new RMCERTAdminEuiState(requests, calls, canForceCalls, forceCalls);
    }

    /// <summary>
    /// Returns ERT call prototypes as public option DTOs using the supplied filters.
    /// This method only queries prototype data; it does not create, mutate, approve, or validate a request.
    /// </summary>
    /// <param name="args">
    /// Query filters for enabled calls, direct admin-selectable calls, and allowed source.
    /// Leave filters unset to list every known ERT call option.
    /// </param>
    /// <returns>Sorted call options suitable for admin UI, commands, or preparing <see cref="RMCERTCreateRequestArgs.AllowedCalls"/>.</returns>
    public List<RMCERTCallOption> GetCallOptions(RMCERTCallQueryArgs args)
    {
        return _prototypes.EnumeratePrototypes<RMCERTCallPrototype>()
            .Where(c => !args.EnabledOnly || c.Enabled)
            .Where(c => !args.AdminSelectableOnly || c.AdminSelectable)
            .Where(c => args.Source == null || c.AllowedSources.Contains(args.Source.Value))
            .OrderBy(c => c.Category)
            .ThenBy(c => c.Name)
            .Select(ToCallOption)
            .ToList();
    }

    private RMCERTCallOption ToCallOption(RMCERTCallPrototype call)
    {
        return new RMCERTCallOption(
            call.ID,
            call.Name,
            GetOrganizationLabel(call),
            call.Category,
            call.RandomWeight,
            call.AdminSelectable,
            call.AdminButtonLabel);
    }

    private void OnAdminPermsChanged(AdminPermsChangedEventArgs args)
    {
        if (!args.IsAdmin)
            return;

        QueuePendingAdminRequestNotification(args.Player, "admin permissions changed");
    }

    private void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs args)
    {
        if (args.NewStatus != SessionStatus.InGame)
            return;

        QueuePendingAdminRequestNotification(args.Session, "session entered game");
    }

    private void QueuePendingAdminRequestNotification(ICommonSession session, string reason)
    {
        if (!_queuedPendingAdminNotifications.Add(session))
        {
            Log.Debug($"Skipped duplicate pending ERT request notification queue for {session.Name}: {reason}.");
            return;
        }

        Log.Debug($"Queued pending ERT request notification for {session.Name}: {reason}.");
        Timer.Spawn(PendingAdminNotificationDelay, () =>
        {
            _queuedPendingAdminNotifications.Remove(session);

            if (session.Status == SessionStatus.Disconnected)
            {
                Log.Debug($"Skipped pending ERT request notification for {session.Name}: session disconnected.");
                return;
            }

            NotifyAdminOfPendingRequests(session);
        });
    }

    private void NotifyAdminOfPendingRequests(ICommonSession session)
    {
        if (!_adminManager.IsAdmin(session))
        {
            Log.Debug($"Skipped pending ERT request notification for {session.Name}: session is not an active admin.");
            return;
        }

        var sent = 0;
        foreach (var request in _requests.Values)
        {
            if (request.State != RMCERTRequestState.PendingAdmin)
                continue;

            _chat.SendAdminAnnouncementMessage(session, BuildAdminRequestAnnouncement(request));
            sent++;
        }

        Log.Debug($"Sent {sent} pending ERT request notifications to {session.Name}.");
    }

    private void NotifyAdminsOfRequest()
    {
        if (!_adminManager.ActiveAdmins.Any())
            return;

        _audio.PlayGlobal(
            AdminRequestSound,
            Filter.Empty().AddPlayers(_adminManager.ActiveAdmins),
            false,
            AudioParams.Default.WithVolume(-6f));
    }
}
