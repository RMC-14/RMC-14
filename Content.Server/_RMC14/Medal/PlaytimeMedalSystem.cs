using Content.Server.Hands.Systems;
using Content.Server.Players.PlayTimeTracking;
using Content.Shared._RMC14.CCVar;
using Content.Shared._RMC14.Medal;
using Content.Shared._RMC14.UniformAccessories;
using Content.Shared.Coordinates;
using Content.Shared.GameTicking;
using Content.Shared.Roles;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;

namespace Content.Server._RMC14.Medal;

public sealed class PlaytimeMedalSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly HandsSystem _hands = default!;
    [Dependency] private readonly PlayTimeTrackingManager _playTimeTracking = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;


    private TimeSpan _bronzeTime;
    private TimeSpan _silverTime;
    private TimeSpan _goldTime;
    private TimeSpan _platinumTime;
    private TimeSpan _rubyTime;
    private TimeSpan _amethystTime;
    private TimeSpan _emeraldTime;
    private TimeSpan _prismaticTime;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawnComplete);

        Subs.CVar(_config, RMCCVars.RMCPlaytimeBronzeMedalTimeHours, v => _bronzeTime = TimeSpan.FromHours(v), true);
        Subs.CVar(_config, RMCCVars.RMCPlaytimeSilverMedalTimeHours, v => _silverTime = TimeSpan.FromHours(v), true);
        Subs.CVar(_config, RMCCVars.RMCPlaytimeGoldMedalTimeHours, v => _goldTime = TimeSpan.FromHours(v), true);
        Subs.CVar(_config, RMCCVars.RMCPlaytimePlatinumMedalTimeHours, v => _platinumTime = TimeSpan.FromHours(v), true);
        Subs.CVar(_config, RMCCVars.RMCPlaytimeRubyMedalTimeHours, v => _rubyTime = TimeSpan.FromHours(v), true);
        Subs.CVar(_config, RMCCVars.RMCPlaytimeAmethystMedalTimeHours, v => _amethystTime = TimeSpan.FromHours(v), true);
        Subs.CVar(_config, RMCCVars.RMCPlaytimeEmeraldMedalTimeHours, v => _emeraldTime = TimeSpan.FromHours(v), true);
        Subs.CVar(_config, RMCCVars.RMCPlaytimePrismaticMedalTimeHours, v => _prismaticTime = TimeSpan.FromHours(v), true);
    }

    private void OnPlayerSpawnComplete(PlayerSpawnCompleteEvent ev)
    {
        if (!ev.Profile.PlaytimePerks)
            return;

        if (ev.JobId == null ||
            !_prototype.TryIndex(ev.JobId, out JobPrototype? job) ||
            !_playTimeTracking.TryGetTrackerTime(ev.Player, job.PlayTimeTracker, out var time))
        {
            return;
        }

        if (job.Medals is not { } medals)
            return;

        RMCPlaytimeMedalType? medalType = null;

        if (time >= _prismaticTime)
            medalType = RMCPlaytimeMedalType.Prismatic;
        else if (time >= _emeraldTime)
            medalType = RMCPlaytimeMedalType.Emerald;
        else if (time >= _amethystTime)
            medalType = RMCPlaytimeMedalType.Amethyst;
        else if (time >= _rubyTime)
            medalType = RMCPlaytimeMedalType.Ruby;
        else if (time >= _platinumTime)
            medalType = RMCPlaytimeMedalType.Platinum;
        else if (time >= _goldTime)
            medalType = RMCPlaytimeMedalType.Gold;
        else if (time >= _silverTime)
            medalType = RMCPlaytimeMedalType.Silver;
        else if (time >= _bronzeTime)
            medalType = RMCPlaytimeMedalType.Bronze;

        if (medalType == null)
            return;

        if (!medals.TryGetValue(medalType.Value, out var medalId))
            return;

        var medal = SpawnAtPosition(medalId, ev.Mob.ToCoordinates());
        _hands.TryPickupAnyHand(ev.Mob, medal, false);
        var medalComp = EnsureComp<UniformAccessoryComponent>(medal);
        medalComp.User = GetNetEntity(ev.Mob);
        Dirty(medal, medalComp);
    }
}
