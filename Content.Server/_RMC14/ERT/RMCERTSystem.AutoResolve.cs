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

    private RMCERTAutoResolutionOptions? GetConsoleAutoResolutionOptions()
    {
        var delaySeconds = _config.GetCVar(RMCCVars.RMCERTConsoleAutoResolveDelaySeconds);
        if (delaySeconds <= 0 || float.IsNaN(delaySeconds) || float.IsInfinity(delaySeconds))
            return null;

        return new RMCERTAutoResolutionOptions
        {
            Delay = TimeSpan.FromSeconds(delaySeconds),
            ApprovalChance = _config.GetCVar(RMCCVars.RMCERTConsoleAutoApproveChance),
            ActorName = Loc.GetString("rmc-ert-admin-actor-auto-resolve"),
        };
    }

    private TimeSpan? GetAutoResolveAt(RMCERTAutoResolutionOptions? options)
    {
        if (options == null || options.Delay <= TimeSpan.Zero)
            return null;

        return _timing.CurTime + options.Delay;
    }

    private string GetAutoResolveActorName(RMCERTAutoResolutionOptions? options)
    {
        if (options == null || options.Delay <= TimeSpan.Zero)
            return string.Empty;

        return !string.IsNullOrWhiteSpace(options.ActorName)
            ? options.ActorName.Trim()
            : Loc.GetString("rmc-ert-admin-actor-auto-resolve");
    }

    private void TryAutoResolvePendingRequest(RMCERTRequest request)
    {
        if (request.State != RMCERTRequestState.PendingAdmin ||
            request.AutoResolveAt is not { } autoResolveAt ||
            _timing.CurTime < autoResolveAt)
        {
            return;
        }

        request.AutoResolveAt = null;

        var actorName = !string.IsNullOrWhiteSpace(request.AutoResolveActorName)
            ? request.AutoResolveActorName
            : Loc.GetString("rmc-ert-admin-actor-auto-resolve");

        if (!_random.Prob(request.AutoApproveChance))
        {
            DenyRequest(new RMCERTRequestActionArgs
            {
                Request = request.Id,
                ActorName = actorName,
            });
            return;
        }

        var result = ApproveRequest(new RMCERTApproveRequestArgs
        {
            Request = request.Id,
            ActorName = actorName,
        });

        if (!result.Success && TryGetPending(request.Id, out var pendingRequest))
        {
            DenyRequest(new RMCERTRequestActionArgs
            {
                Request = pendingRequest.Id,
                ActorName = actorName,
            });
        }
    }

    private static float ClampProbability(float probability)
    {
        return float.IsNaN(probability)
            ? 0f
            : Math.Clamp(probability, 0f, 1f);
    }
}
