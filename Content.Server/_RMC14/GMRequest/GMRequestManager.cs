using System.Threading;
using Content.Server.Administration.Logs;
using Content.Server.Chat.Managers;
using Content.Shared._RMC14.CCVar;
using Content.Shared._RMC14.GMRequest;
using Content.Shared._RMC14.Roles;
using Content.Shared.Ghost;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._RMC14.GMRequest;
/// <summary>
/// Handles the storage and modification of GM Request Logs
/// </summary>
/// <remarks>
/// The "true" location of GM Request log data.
/// </remarks>
public sealed class GMRequestManager
{
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly Robust.Shared.Configuration.IConfigurationManager _cfg = default!;
    [Dependency] private readonly IEntityManager _entMan = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    public Dictionary<int, GMRequestLog> Logs { get; } = new();

    private int _currentLogId;
    private int NextLogId => Interlocked.Increment(ref _currentLogId);

    //Events are used to ensure all client EUIs receive the update, regardless of who or what caused it
    public event Action<int, bool>? LogUpdate;
    public event Action? LogsCleared;

    public void Add(ICommonSession sender, string message)
    {
        var entityname = Loc.GetString("rmc-gm-request-log-entity-default");

        //Determine entity name and job, if possible
        if (_entMan.TryGetComponent(sender.AttachedEntity, out MetaDataComponent? metaData))
        {
            var job = string.Empty;
            if (_entMan.TryGetComponent(sender.AttachedEntity, out OriginalRoleComponent? rolecomp))
            {
                if (_prototype.TryIndex(rolecomp.Job, out var jobproto))
                    job = $" ({Loc.GetString(jobproto.Name)})";
            }

            entityname =
                _entMan.HasComponent<GhostComponent>(sender.AttachedEntity)
                    ? Loc.GetString("rmc-gm-request-log-entity-ghost")
                    : $"{metaData.EntityName}{job}";
        }

        //Truncate messages that are too long
        if (message.Length > _cfg.GetCVar(RMCCVars.RMCGMRequestMaxLength))
        {
            message = message[.._cfg.GetCVar(RMCCVars.RMCGMRequestMaxLength)];
        }

        var log = new GMRequestLog(
            sender.UserId,
            sender.Name,
            entityname,
            message,
            DateTime.Now,
            null,
            false
        );

        Logs.Add(NextLogId, log);
        LogUpdate?.Invoke(_currentLogId, true);
        _chatManager.SendAdminAnnouncement($"REQUEST <{sender.Name}> has sent a request!");
    }


    public void Claim(int id, string? claimant)
    {
        var log = Logs[id];
        log.ClaimName = claimant;
        Logs[id] = log;
        LogUpdate?.Invoke(id, false);
    }

    public void Hide(int id)
    {
        var log = Logs[id];
        log.Hidden = !log.Hidden;
        Logs[id] = log;
        LogUpdate?.Invoke(id, false);
    }

    public void ClearGMRequests()
    {
        Logs.Clear();
        _currentLogId = 0;
        LogsCleared?.Invoke();
    }
}
