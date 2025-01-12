using Content.Server.Antag;
using Content.Server.Communications;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Server.RoundEnd;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Popups;
using Content.Server._WH14K.WarpShtorm;
using Content.Server._WH14K.Altar;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.GameTicking.Components;
using Robust.Shared.Utility;
using Robust.Shared.Map.Components;
using Content.Shared.Mind;
using Robust.Server.Player;

namespace Content.Server._WH14K.GameTicking.Rules;

public sealed partial class PlanetaryWarfareRuleSystem : GameRuleSystem<PlanetaryWarfareRuleComponent>
{
    [Dependency] private readonly RoundEndSystem _roundEndSystem = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CommandIGComponent, ComponentRemove>(OnComponentRemove);
        SubscribeLocalEvent<CommandIGComponent, MobStateChangedEvent>(OnMobStateChanged);
    }

    protected override void AppendRoundEndText(EntityUid uid, PlanetaryWarfareRuleComponent component, GameRuleComponent gameRule, ref RoundEndTextAppendEvent args)
    {
        switch (component.WinTypePW)
        {
            case WinTypePW.WarpStormSummoned:
                args.AddLine(Loc.GetString("pw-chaos-win"));
                args.AddLine(Loc.GetString("pw-warp-storm-summoned-desc"));
                break;
            case WinTypePW.AllCommandDead:
                args.AddLine(Loc.GetString("pw-chaos-win"));
                args.AddLine(Loc.GetString("pw-all-command-dead-desc"));
                break;
            case WinTypePW.AllAltarExploded:
                args.AddLine(Loc.GetString("pw-ig-win"));
                args.AddLine(Loc.GetString("pw-all-altars-exploded-desc"));
                break;
            default:
                break;
        }

        var igCommand = EntityQuery<CommandIGComponent, MindComponent>(true);
        foreach (var (ig, mind) in igCommand)
        {
            if (mind is null || mind.CharacterName is null || mind.OriginalOwnerUserId is null)
                return;

            _player.TryGetPlayerData(mind.OriginalOwnerUserId.Value, out var data);

            if (data == null)
                return;
            args.AddLine(Loc.GetString("ig-command-list-start"));

            args.AddLine(Loc.GetString("ig-command-list-name-user", ("name", mind.CharacterName), ("user", data.UserName)));
        }
    }

    private void OnComponentRemove(EntityUid uid, CommandIGComponent component, ComponentRemove args)
    {
        CheckRoundShouldEnd();
    }

    private void OnMobStateChanged(EntityUid uid, CommandIGComponent component, MobStateChangedEvent ev)
    {
        if (ev.NewMobState == MobState.Dead)
            CheckRoundShouldEnd();
    }

    public void CheckRoundShouldEnd()
    {
        var query = QueryActiveRules();
        while (query.MoveNext(out var uid, out _, out var pw, out _))
        {
            WarpStormSummoned((uid, pw));
            AllCommandDead((uid, pw));
            AllAltarExploded((uid, pw));
        }
    }

    private void AllCommandDead(Entity<PlanetaryWarfareRuleComponent> ent)
    {
        var igCommand = EntityQuery<CommandIGComponent, MobStateComponent>(true);

        foreach (var (ig, igMobState) in igCommand)
        {
            if (igMobState.CurrentState == MobState.Alive)
            {
                return;
            }
        }

        ent.Comp.WinTypePW = WinTypePW.AllCommandDead;
        _roundEndSystem.EndRound();
    }

    private void AllAltarExploded(Entity<PlanetaryWarfareRuleComponent> ent)
    {
        var altars = EntityQuery<AltarComponent>(true);

        foreach (var altar in altars)
        {
            if (!altar.Exploded)
            {
                return;
            }
        }

        ent.Comp.WinTypePW = WinTypePW.AllAltarExploded;
        _roundEndSystem.EndRound();
    }

    private void WarpStormSummoned(Entity<PlanetaryWarfareRuleComponent> ent)
    {
        var warpStorms = EntityQuery<WarpStormComponent>();
        foreach (var warpStorm in warpStorms)
        {
            var mapQuery = EntityQueryEnumerator<MapLightComponent>();
            while (mapQuery.MoveNext(out var uid, out var map))
            {
                map.AmbientLightColor = Color.FromHex("#e82a2a");
                Dirty(uid, map);
            }

            ent.Comp.WinTypePW = WinTypePW.WarpStormSummoned;
            _roundEndSystem.EndRound();
        }
    }
}
