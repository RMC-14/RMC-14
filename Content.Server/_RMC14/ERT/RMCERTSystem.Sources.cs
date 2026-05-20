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

    private void OnMarineCommunicationsDistressBeacon(Entity<MarineCommunicationsComputerComponent> ent, ref MarineCommunicationsDistressBeaconMsg args)
    {
        if (!CanUseConsoleDistress(ent, out var reason))
        {
            _popup.PopupEntity(reason, ent, args.Actor, PopupType.MediumCaution);
            return;
        }

        var ev = new RMCERTConsoleDistressReasonEvent(GetNetEntity(args.Actor));
        _dialog.OpenInput(ent, args.Actor, Loc.GetString("rmc-ert-prompt-console-reason"), ev, true, ent.Comp.DistressReasonLimit);
    }

    private void OnConsoleReason(Entity<MarineCommunicationsComputerComponent> ent, ref RMCERTConsoleDistressReasonEvent args)
    {
        if (!TryGetEntity(args.User, out var user))
            return;

        if (!CanUseConsoleDistress(ent, out var reason))
        {
            _popup.PopupEntity(reason, ent, user.Value, PopupType.MediumCaution);
            return;
        }

        CreateConsoleDistressRequest(ent, user.Value, args.Message);
    }

    private bool CanUseConsoleDistress(Entity<MarineCommunicationsComputerComponent> console, out string reason)
    {
        if (!console.Comp.CanTransmitDistress)
        {
            reason = Loc.GetString("rmc-ert-popup-console-unavailable");
            return false;
        }

        if (!_alertLevel.IsRedOrDeltaAlert())
        {
            reason = Loc.GetString("rmc-ert-popup-console-alert-required");
            return false;
        }

        if (!CanCreateRequest(console, out reason))
            return false;

        return true;
    }

    private void OnERTMindAdded(Entity<RMCERTMemberComponent> ent, ref MindAddedMessage args)
    {
        if (!TryComp(ent, out ActorComponent? actor) ||
            !_requests.TryGetValue(ent.Comp.RequestId, out var request) ||
            !_prototypes.TryIndex(new ProtoId<RMCERTCallPrototype>(ent.Comp.Call), out var call))
        {
            return;
        }

        var briefing = BuildMemberBriefing(request, call);
        if (string.IsNullOrWhiteSpace(briefing))
            return;

        _chat.DispatchServerMessage(actor.PlayerSession, briefing, false);
    }

    private void OnHandheldUse(Entity<RMCERTDistressBeaconComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        if (TryComp(ent, out AccessReaderComponent? access) &&
            !_access.IsAllowed(args.User, ent, access))
        {
            _popup.PopupEntity(Loc.GetString("rmc-access-denied"), ent, args.User, PopupType.MediumCaution);
            args.Handled = true;
            return;
        }

        if (!CanCreateRequest(ent, out var reason))
        {
            _popup.PopupEntity(reason, ent, args.User, PopupType.MediumCaution);
            args.Handled = true;
            return;
        }

        if (ent.Comp.ReasonRequired)
        {
            // Attach the dialog to the user, not the held item. Keep the beacon association server-side so the
            // networked dialog state does not need to serialize an inventory-entity reference back to the client.
            _pendingHandheldDialogs[args.User] = ent;
            var ev = new RMCERTHandheldDistressReasonEvent();
            _dialog.OpenInput(args.User, Loc.GetString("rmc-ert-prompt-handheld-reason", ("title", ent.Comp.RequestTitle)), ev, true, ent.Comp.ReasonLimit);
        }
        else
        {
            CreateHandheldDistressRequest(ent, args.User, string.Empty);
        }

        args.Handled = true;
    }

    private void OnHandheldReason(Entity<ActorComponent> ent, ref RMCERTHandheldDistressReasonEvent args)
    {
        if (_timing.ApplyingState)
            return;

        if (!_pendingHandheldDialogs.Remove(ent.Owner, out var beaconUid) ||
            !TryComp(beaconUid, out RMCERTDistressBeaconComponent? beacon))
        {
            return;
        }

        CreateHandheldDistressRequest((beaconUid, beacon), ent.Owner, args.Message);
    }

    private void CreateConsoleDistressRequest(EntityUid console, EntityUid user, string reason)
    {
        var calls = GetCallOptions(new RMCERTCallQueryArgs
        {
            EnabledOnly = true,
            Source = RMCERTRequestSource.Console,
        }).Select(c => new ProtoId<RMCERTCallPrototype>(c.Id)).ToList();
        var autoResolution = GetConsoleAutoResolutionOptions();

        var result = CreateRequest(new RMCERTCreateRequestArgs
        {
            Source = RMCERTRequestSource.Console,
            SourceEntity = console,
            Requester = user,
            Reason = reason,
            AllowedCalls = calls,
            AllowRandomSelection = true,
            AllowSpecificSelection = false,
            AutoResolution = autoResolution,
        });

        if (!result.Success && !string.IsNullOrWhiteSpace(result.Error))
            _popup.PopupEntity(result.Error, console, user, PopupType.MediumCaution);

        _ui.CloseUi(console, MarineCommunicationsComputerUI.Key, user);
    }

    private void CreateHandheldDistressRequest(Entity<RMCERTDistressBeaconComponent> beacon, EntityUid user, string reason)
    {
        reason = reason.Trim();

        if (beacon.Comp.Spent)
        {
            _popup.PopupEntity(Loc.GetString("rmc-ert-popup-beacon-spent"), beacon, user, PopupType.MediumCaution);
            return;
        }

        if (_timing.CurTime < beacon.Comp.LastUsed + beacon.Comp.Cooldown)
        {
            _popup.PopupEntity(Loc.GetString("rmc-ert-popup-beacon-cooldown"), beacon, user, PopupType.MediumCaution);
            return;
        }

        if (beacon.Comp.ReasonRequired && string.IsNullOrWhiteSpace(reason))
        {
            _popup.PopupEntity(Loc.GetString("rmc-ert-popup-beacon-reason-required"), beacon, user, PopupType.MediumCaution);
            return;
        }

        if (!TryGetHandheldAvailableCalls(beacon, user, reason, out var calls, out var error))
        {
            _popup.PopupEntity(error, beacon, user, PopupType.MediumCaution);
            return;
        }

        beacon.Comp.LastUsed = _timing.CurTime;

        var result = CreateRequest(new RMCERTCreateRequestArgs
        {
            Source = RMCERTRequestSource.Handheld,
            SourceEntity = beacon,
            Requester = user,
            Reason = reason,
            AllowedCalls = calls,
            AllowRandomSelection = true,
            AllowSpecificSelection = true,
        });

        if (!result.Success && !string.IsNullOrWhiteSpace(result.Error))
            _popup.PopupEntity(result.Error, beacon, user, PopupType.MediumCaution);
    }

    private bool TryGetHandheldAvailableCalls(
        Entity<RMCERTDistressBeaconComponent> beacon,
        EntityUid user,
        string reason,
        out List<ProtoId<RMCERTCallPrototype>> calls,
        out string error)
    {
        calls = new List<ProtoId<RMCERTCallPrototype>>();
        error = Loc.GetString("rmc-ert-error-beacon-no-teams");

        var configuredCalls = beacon.Comp.AllowedCalls.Count == 0
            ? _prototypes.EnumeratePrototypes<RMCERTCallPrototype>()
                .Where(c => c.Enabled && c.AllowedSources.Contains(RMCERTRequestSource.Handheld))
                .Select(c => new ProtoId<RMCERTCallPrototype>(c.ID))
                .ToList()
            : beacon.Comp.AllowedCalls.ToList();

        if (configuredCalls.Count == 0)
            return false;

        var request = new RMCERTRequest
        {
            Source = RMCERTRequestSource.Handheld,
            SourceEntity = beacon,
            Requester = user,
            SourceName = Name(beacon),
            RequesterName = Name(user),
            Reason = reason,
            CreatedAt = _timing.CurTime,
        };

        foreach (var callId in configuredCalls)
        {
            if (!_prototypes.TryIndex(callId, out var call))
                continue;

            if (!call.Enabled || !call.AllowedSources.Contains(RMCERTRequestSource.Handheld))
                continue;

            if (!CheckRequirements(request, call, out var requirementError))
            {
                error = requirementError;
                continue;
            }

            calls.Add(callId);
        }

        return calls.Count > 0;
    }

    private void UpdateSourceVisual(RMCERTRequest request, bool active)
    {
        if (request.SourceEntity is { Valid: true } source &&
            HasComp<RMCERTDistressBeaconComponent>(source))
        {
            UpdateBeaconVisual(source, active);
        }
    }

    private void UpdateBeaconVisual(EntityUid uid, bool active)
    {
        if (TryComp<AppearanceComponent>(uid, out var appearance))
            _appearance.SetData(uid, RMCERTDistressBeaconVisuals.Active, active, appearance);
    }
}
