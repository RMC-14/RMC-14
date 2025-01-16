﻿using Content.Server.Hands.Systems;
using Content.Server.Players.PlayTimeTracking;
using Content.Shared._RMC14.CCVar;
using Content.Shared._RMC14.Marines;
using Content.Shared._RMC14.Medal;
using Content.Shared._RMC14.Survivor;
using Content.Shared.Coordinates;
using Content.Shared.GameTicking;
using Content.Shared.Roles;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;

namespace Content.Server._RMC14.Medal;

public sealed class PlaytimeMedalSystem : SharedPlaytimeMedalSystem
{
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly HandsSystem _hands = default!;
    [Dependency] private readonly PlayTimeTrackingManager _playTimeTracking = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    private static readonly EntProtoId BronzeMedal = "RMCMedalBronzeService";
    private static readonly EntProtoId SilverMedal = "RMCMedalSilverService";
    private static readonly EntProtoId GoldMedal = "RMCMedalGoldService";
    private static readonly EntProtoId PlatinumMedal = "RMCMedalPlatinumService";

    private static readonly EntProtoId WhiteRibbon = "RMCMedalRibbonWhiteService";
    private static readonly EntProtoId YellowRibbon = "RMCMedalRibbonYellowService";
    private static readonly EntProtoId RedRibbon = "RMCMedalRibbonRedService";
    private static readonly EntProtoId BlueRibbon = "RMCMedalRibbonBlueService";

    private TimeSpan _bronzeTime;
    private TimeSpan _silverTime;
    private TimeSpan _goldTime;
    private TimeSpan _platinumTime;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawnComplete);

        Subs.CVar(_config, RMCCVars.RMCPlaytimeBronzeMedalTimeHours, v => _bronzeTime = TimeSpan.FromHours(v), true);
        Subs.CVar(_config, RMCCVars.RMCPlaytimeSilverMedalTimeHours, v => _silverTime = TimeSpan.FromHours(v), true);
        Subs.CVar(_config, RMCCVars.RMCPlaytimeGoldMedalTimeHours, v => _goldTime = TimeSpan.FromHours(v), true);
        Subs.CVar(_config, RMCCVars.RMCPlaytimePlatinumMedalTimeHours, v => _platinumTime = TimeSpan.FromHours(v), true);
    }

    private void OnPlayerSpawnComplete(PlayerSpawnCompleteEvent ev)
    {
        if (!ev.Profile.PlaytimePerks)
            return;

        if (HasComp<SurvivorComponent>(ev.Mob))
            return;

        if (ev.JobId == null ||
            !_prototype.TryIndex(ev.JobId, out JobPrototype? job) ||
            !_playTimeTracking.TryGetTrackerTime(ev.Player, job.PlayTimeTracker, out var time))
        {
            return;
        }

        EntProtoId? medalId = null;
        if (HasComp<MarineComponent>(ev.Mob))
        {
            if (time >= _platinumTime)
                medalId = PlatinumMedal;
            else if (time >= _goldTime)
                medalId = GoldMedal;
            else if (time >= _silverTime)
                medalId = SilverMedal;
            else if (time >= _bronzeTime)
                medalId = BronzeMedal;
        }
        else if (!HasComp<SurvivorComponent>(ev.Mob))
        {
            if (time >= _platinumTime)
                medalId = BlueRibbon;
            else if (time >= _goldTime)
                medalId = RedRibbon;
            else if (time >= _silverTime)
                medalId = YellowRibbon;
            else if (time >= _bronzeTime)
                medalId = WhiteRibbon;
        }

        if (medalId == null)
            return;

        var medal = SpawnAtPosition(medalId, ev.Mob.ToCoordinates());
        _hands.TryPickupAnyHand(ev.Mob, medal, false);

        EnsureComp<PlaytimeMedalUserComponent>(ev.Mob);

        var medalComp = EnsureComp<PlaytimeMedalComponent>(medal);
        medalComp.User = ev.Mob;
        Dirty(medal, medalComp);
    }
}
