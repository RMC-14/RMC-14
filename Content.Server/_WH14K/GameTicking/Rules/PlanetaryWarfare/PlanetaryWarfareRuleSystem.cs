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

        SubscribeLocalEvent<IGCommandComponent, ComponentRemove>(OnComponentRemove);
        SubscribeLocalEvent<IGCommandComponent, MobStateChangedEvent>(OnMobStateChanged);
    }

    protected override void AppendRoundEndText(EntityUid uid, PlanetaryWarfareRuleComponent component, GameRuleComponent gameRule, ref RoundEndTextAppendEvent args)
    {
        if (component.WinTypePW == WinTypePW.WarpShtormSummoned || component.WinTypePW == WinTypePW.AllCommandDead)
            args.AddLine(Loc.GetString("pw-chaos-win"));

        else if (component.WinTypePW == WinTypePW.AllAltarExploded)
            args.AddLine(Loc.GetString("pw-ig-win"));

        if (component.WinTypePW == WinTypePW.AllCommandDead)
            args.AddLine(Loc.GetString("pw-all-command-dead-desc"));

        else if (component.WinTypePW == WinTypePW.AllAltarExploded)
            args.AddLine(Loc.GetString("pw-all-altars-exploded-desc"));

        else if (component.WinTypePW == WinTypePW.WarpShtormSummoned)
            args.AddLine(Loc.GetString("pw-all-warp-shtorm-summoned-desc"));

        var IG = EntityQuery<IGCommandComponent, MindComponent>(true);
        foreach (var i in IG)
        {
            if (i.Item2 == null || i.Item2.CharacterName == null || i.Item2.OriginalOwnerUserId == null )
                return;

            _player.TryGetPlayerData(i.Item2.OriginalOwnerUserId.Value, out var data);

            if (data == null)
                return;
            args.AddLine(Loc.GetString("ig-command-list-start"));

            args.AddLine(Loc.GetString("ig-command-list-name-user", ("name", i.Item2.CharacterName), ("user", data.UserName)));
        }
    }

    private void OnComponentRemove(EntityUid uid, IGCommandComponent component, ComponentRemove args)
    {
        CheckRoundShouldEnd();
    }

    private void OnMobStateChanged(EntityUid uid, IGCommandComponent component, MobStateChangedEvent ev)
    {
        if (ev.NewMobState == MobState.Dead)
            CheckRoundShouldEnd();
    }

    public void CheckRoundShouldEnd()
    {
        var query = QueryActiveRules();
        while (query.MoveNext(out var uid, out _, out var pw, out _))
        {
            AllCommandDead((uid, pw));
            AllAltarExploded((uid, pw));
            OnWarpShtormSummoned((uid, pw));
        }
    }

    private void AllCommandDead(Entity<PlanetaryWarfareRuleComponent> ent)
    {
        var IG = EntityQuery<IGCommandComponent, MobStateComponent>(true);

        foreach (var i in IG)
        {
            if (i.Item2.CurrentState == MobState.Alive)
            {
                return;
            }
        }

        ent.Comp.WinTypePW = WinTypePW.AllCommandDead;
        _roundEndSystem.EndRound();
    }

    private void AllAltarExploded(Entity<PlanetaryWarfareRuleComponent> ent)
    {
        var Altar = EntityQuery<AltarComponent>(true);

        foreach (var a in Altar)
        {
            if (!a.Exploded)
            {
                return;
            }
        }

        ent.Comp.WinTypePW = WinTypePW.AllAltarExploded;
        _roundEndSystem.EndRound();
    }

    private void OnWarpShtormSummoned(Entity<PlanetaryWarfareRuleComponent> ent)
    {
        var WarpShtorm = EntityQuery<WarpShtormComponent>();
        foreach (var ws in WarpShtorm)
        {
            var mapQuery = EntityQueryEnumerator<MapLightComponent>();
            while (mapQuery.MoveNext(out var uid, out var map))
            {
                map.AmbientLightColor = Color.FromHex("#e82a2a");
                Dirty(uid, map);
            }

            ent.Comp.WinTypePW = WinTypePW.WarpShtormSummoned;
            _roundEndSystem.EndRound();
        }
    }
}
