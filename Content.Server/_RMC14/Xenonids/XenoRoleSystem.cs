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
using Robust.Server.GameStates;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server._RMC14.Xenonids;

public sealed class XenoRoleSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly SharedXenoHiveSystem _hive = default!;
    [Dependency] private readonly XenoInfectionsManager _infectionsManager = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly NameModifierSystem _nameModifier = default!;
    [Dependency] private readonly PlayTimeTrackingSystem _playTime = default!;
    [Dependency] private readonly PvsOverrideSystem _pvsOverride = default!;
    [Dependency] private readonly RoleSystem _role = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private TimeSpan _disconnectedXenoGhostRoleTime;

    private int _rankMatureThreshold;
    private int _rankElderThreshold;
    private int _rankAncientThreshold;
    private int _rankPrimeThreshold;

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

        Subs.CVar(_config, RMCCVars.RMCXenoInfectRankMatureThreshold, v => _rankMatureThreshold = v, true);
        Subs.CVar(_config, RMCCVars.RMCXenoInfectRankElderThreshold, v => _rankElderThreshold = v, true);
        Subs.CVar(_config, RMCCVars.RMCXenoInfectRankAncientThreshold, v => _rankAncientThreshold = v, true);
        Subs.CVar(_config, RMCCVars.RMCXenoInfectRankPrimeThreshold, v => _rankPrimeThreshold = v, true);
        Subs.CVar(_config, RMCCVars.RMCDisconnectedXenoGhostRoleTimeSeconds, v => _disconnectedXenoGhostRoleTime = TimeSpan.FromSeconds(v), true);
    }

    private void OnPlayerSpawnComplete(PlayerSpawnCompleteEvent ev)
    {
        if (ev.JobId != null)
            UpdateRank(ev.Mob, ev.Player, ev.Profile);
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

    private void UpdateRank(EntityUid xeno, ICommonSession player, HumanoidCharacterProfile profile)
    {
        if (!HasComp<XenoComponent>(xeno))
            return;

        var infects = _infectionsManager.GetInfects(player.UserId);

        int rank;
        if (!profile.PlaytimePerks)
            rank = 1;
        else if (infects >= _rankPrimeThreshold)
            rank = 5;
        else if (infects >= _rankAncientThreshold)
            rank = 4;
        else if (infects >= _rankElderThreshold)
            rank = 3;
        else if (infects >= _rankMatureThreshold)
            rank = 2;
        else
            rank = 0;

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
                        UpdateRank(xeno, player, profile);
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
