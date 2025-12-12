using Content.Server.GameTicking;
using Content.Server.Mind;
using Content.Server.Players.PlayTimeTracking;
using Content.Server.Roles;
using Content.Shared._RMC14.CCVar;
using Content.Shared._RMC14.Xenonids;
using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared._RMC14.Xenonids.Maturing;
using Content.Shared._RMC14.Xenonids.Name;
using Content.Shared._RMC14.Xenonids.Rank;
using Content.Shared.GameTicking;
using Content.Shared.NameModifier.EntitySystems;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Robust.Server.GameStates;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._RMC14.Xenonids;

public sealed class XenoRoleSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly SharedXenoHiveSystem _hive = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly NameModifierSystem _nameModifier = default!;
    [Dependency] private readonly PlayTimeTrackingSystem _playTime = default!;
    [Dependency] private readonly PlayTimeTrackingManager _playTimeManager = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly PvsOverrideSystem _pvsOverride = default!;
    [Dependency] private readonly RoleSystem _role = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private TimeSpan _disconnectedXenoGhostRoleTime;

    private TimeSpan _rankTwoTime;
    private TimeSpan _rankThreeTime;
    private TimeSpan _rankFourTime;
    private TimeSpan _rankFiveTime;
    private TimeSpan _rankSixTime;

    private readonly List<Entity<XenoComponent>> _toUpdate = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawnComplete);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestartCleanup);

        SubscribeLocalEvent<XenoComponent, PlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<XenoComponent, PlayerDetachedEvent>(OnPlayerDetached);

        SubscribeLocalEvent<ActorComponent, HiveChangedEvent>(OnHiveChanged);

        SubscribeLocalEvent<XenoRankComponent, RefreshNameModifiersEvent>(OnRankRefreshName, before: new[] { typeof(SharedXenoNameSystem) });

        Subs.CVar(_config, RMCCVars.RMCPlaytimeBronzeMedalTimeHours, v => _rankTwoTime = TimeSpan.FromHours(v), true);
        Subs.CVar(_config, RMCCVars.RMCPlaytimeSilverMedalTimeHours, v => _rankThreeTime = TimeSpan.FromHours(v), true);
        Subs.CVar(_config, RMCCVars.RMCPlaytimeGoldMedalTimeHours, v => _rankFourTime = TimeSpan.FromHours(v), true);
        Subs.CVar(_config, RMCCVars.RMCPlaytimePlatinumMedalTimeHours, v => _rankFiveTime = TimeSpan.FromHours(v), true);
        Subs.CVar(_config, RMCCVars.RMCPlaytimeRubyMedalTimeHours, v => _rankSixTime = TimeSpan.FromHours(v), true);
        Subs.CVar(_config, RMCCVars.RMCDisconnectedXenoGhostRoleTimeSeconds, v => _disconnectedXenoGhostRoleTime = TimeSpan.FromSeconds(v), true);
    }

    private void OnPlayerSpawnComplete(PlayerSpawnCompleteEvent ev)
    {
        if (ev.JobId is { } job)
            UpdateRank(ev.Mob, ev.Player, job, ev.Profile);
    }

    private void OnRoundRestartCleanup(RoundRestartCleanupEvent ev)
    {
        _toUpdate.Clear();
    }

    private void OnPlayerAttached(Entity<XenoComponent> xeno, ref PlayerAttachedEvent args)
    {
        RemCompDeferred<XenoDisconnectedComponent>(xeno);
        _toUpdate.Add(xeno);
    }

    private void OnPlayerDetached(Entity<XenoComponent> xeno, ref PlayerDetachedEvent args)
    {
        if(TerminatingOrDeleted(xeno))
            return;

        var disconnected = EnsureComp<XenoDisconnectedComponent>(xeno);
        disconnected.At = _timing.CurTime;

        if (_hive.GetHive(xeno.Owner) is {} hive)
            _pvsOverride.RemoveForceSend(hive, args.Player);
    }

    private void OnHiveChanged(Entity<ActorComponent> ent, ref HiveChangedEvent args)
    {
        if (ent.Comp.PlayerSession is not {} session)
            return;

        if (args.OldHive is {} oldHive)
            _pvsOverride.RemoveForceSend(oldHive, session);

        if (args.Hive is {} newHive)
            _pvsOverride.AddForceSend(newHive, session);
    }

    private void OnRankRefreshName(Entity<XenoRankComponent> ent, ref RefreshNameModifiersEvent args)
    {
        if (HasComp<XenoMaturingComponent>(ent) || !TryComp<XenoRankNamesComponent>(ent, out var rankNamesComp))
            return;

        LocId? rank = null;

        if (rankNamesComp.RankNames.ContainsKey(ent.Comp.Rank))
            rank = rankNamesComp.RankNames[ent.Comp.Rank];

        if (rank == null)
            return;

        args.AddModifier(rank.Value);
    }

    private void UpdateRank(EntityUid xeno, ICommonSession player, string jobId, HumanoidCharacterProfile profile)
    {
        if (!HasComp<XenoComponent>(xeno))
            return;

        var time = TimeSpan.Zero;
        if (_prototype.TryIndex(jobId, out JobPrototype? job) &&
            _playTimeManager.TryGetTrackerTime(player, job.PlayTimeTracker, out var nullableTime))
        {
            time = nullableTime.Value;
        }

        int rank;
        if (!profile.PlaytimePerks)
            rank = 1;
        else if (time > _rankSixTime)
            rank = 6;
        else if (time > _rankFiveTime)
            rank = 5;
        else if (time > _rankFourTime)
            rank = 4;
        else if (time > _rankThreeTime)
            rank = 3;
        else if (time > _rankTwoTime)
            rank = 2;
        else
            rank = 0;

        // TODO RMC14 names
        var rankComp = EnsureComp<XenoRankComponent>(xeno);
        rankComp.Rank = rank;
        Dirty(xeno, rankComp);

        _nameModifier.RefreshNameModifiers(xeno);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        try
        {
            for (var i = _toUpdate.Count - 1; i >= 0; i--)
            {
                var removed = false;
                try
                {
                    var xeno = _toUpdate[i];
                    if (TerminatingOrDeleted(xeno))
                    {
                        removed = true;
                        _toUpdate.RemoveAt(i);
                    }

                    if (!TryComp(xeno, out ActorComponent? actor))
                        continue;

                    var player = actor.PlayerSession;
                    if (!_mind.TryGetMind(player.UserId, out var mind))
                        continue;

                    if (_hive.GetHive(xeno.Owner) is { } hive)
                        _pvsOverride.AddForceSend(hive, player);

                    _role.MindAddJobRole(mind.Value, jobPrototype: xeno.Comp.Role);
                    _playTime.PlayerRolesChanged(player);

                    try
                    {
                        var profile = _gameTicker.GetPlayerProfile(player);
                        UpdateRank(xeno, player, xeno.Comp.Role, profile);
                    }
                    catch (Exception e)
                    {
                        Log.Error($"Error setting xeno rank for {ToPrettyString(xeno)}:\n{e}");
                    }
                }
                finally
                {
                    if (!removed)
                        _toUpdate.RemoveAt(i);
                }
            }
        }
        catch (Exception e)
        {
            Log.Error($"Error processing list of xenos to update:\n{e}");
        }

        var time = _timing.CurTime;
        var disconnectedQuery = EntityQueryEnumerator<XenoDisconnectedComponent>();
        while (disconnectedQuery.MoveNext(out var uid, out var comp))
        {
            if (time < comp.At + _disconnectedXenoGhostRoleTime)
                continue;

            if (!_mind.TryGetMind(uid, out var mindId, out var mind))
            {
                RemCompDeferred<XenoDisconnectedComponent>(uid);
                continue;
            }

            _mind.TransferTo(mindId, null, createGhost: true, mind: mind);
            RemCompDeferred<XenoDisconnectedComponent>(uid);
        }
    }
}
