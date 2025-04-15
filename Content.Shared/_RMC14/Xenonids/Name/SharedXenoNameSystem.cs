using Content.Shared._RMC14.CCVar;
using Content.Shared._RMC14.Xenonids.Evolution;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.NameModifier.EntitySystems;
using Content.Shared.Players.PlayTimeTracking;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Xenonids.Name;

public abstract class SharedXenoNameSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly NameModifierSystem _nameModifier = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly ISharedPlaytimeManager _playtime = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    private TimeSpan _xenoPrefixThreeTime;
    private TimeSpan _xenoPostfixTime;
    private TimeSpan _xenoPostfixTwoTime;

    public override void Initialize()
    {
        SubscribeLocalEvent<NewXenoEvolvedEvent>(OnNewXenoEvolved);
        SubscribeLocalEvent<XenoDevolvedEvent>(OnXenoDevolved);

        SubscribeLocalEvent<XenoNameComponent, RefreshNameModifiersEvent>(OnRefreshNameModifiers);
        SubscribeLocalEvent<XenoNameComponent, MindAddedMessage>(OnMindAdded);

        Subs.CVar(_config,
            RMCCVars.RMCPlaytimeXenoPrefixThreeTimeHours,
            v => _xenoPrefixThreeTime = TimeSpan.FromHours(v),
            true);
        Subs.CVar(_config,
            RMCCVars.RMCPlaytimeXenoPostfixTimeHours,
            v => _xenoPostfixTime = TimeSpan.FromHours(v),
            true);
        Subs.CVar(_config,
            RMCCVars.RMCPlaytimeXenoPostfixTwoTimeHours,
            v => _xenoPostfixTwoTime = TimeSpan.FromHours(v),
            true);
    }

    private void OnNewXenoEvolved(ref NewXenoEvolvedEvent ev)
    {
        TransferName(ev.OldXeno, ev.NewXeno);
    }

    private void OnXenoDevolved(ref XenoDevolvedEvent ev)
    {
        TransferName(ev.OldXeno, ev.NewXeno);
    }

    private void OnRefreshNameModifiers(Entity<XenoNameComponent> ent, ref RefreshNameModifiersEvent args)
    {
        var rank = ent.Comp.Rank;
        if (rank.Length > 0)
            rank = $"{rank} ";

        var prefix = ent.Comp.Prefix;
        if (prefix.Length == 0)
            prefix = "XX";

        var postfix = ent.Comp.Postfix;

        var number = ent.Comp.Number;

        if (HasComp<XenoOmitNumberComponent>(ent))
        {
            args.AddModifier("rmc-xeno-name", extraArgs: [("rank", rank), ("prefix", prefix), ("postfix", postfix)]);
        }
        else
        {
            if (postfix.Length > 0)
                postfix = $"-{postfix}";

            args.AddModifier("rmc-xeno-name-number", extraArgs: [("rank", rank), ("prefix", prefix), ("number", number), ("postfix", postfix)]);
        }

        if (_mind.TryGetMind(ent, out _, out var mind))
            mind.CharacterName = args.GetModifiedName();
    }

    private void OnMindAdded(EntityUid uid, XenoNameComponent component, MindAddedMessage args)
    {
        SetupName(uid);
    }

    private TimeSpan GetXenoPlaytime(ICommonSession player)
    {
        var xenoPlaytime = TimeSpan.Zero;
        try
        {
            var times = _playtime.GetPlayTimes(player);
            foreach (var (id, time) in times)
            {
                if (_prototype.TryIndex(id, out PlayTimeTrackerPrototype? tracker) &&
                    tracker.IsXeno)
                {
                    xenoPlaytime += time;
                }
            }
        }
        catch (Exception e)
        {
            Log.Error($"Error reading total xeno playtime:\n{e}");
        }

        return xenoPlaytime;
    }

    public int GetMaxXenoPrefixLength(ICommonSession player)
    {
        return GetXenoPlaytime(player) < _xenoPrefixThreeTime ? 2 : 3;
    }

    public int GetMaxXenoPostfixLength(ICommonSession player)
    {
        var time = GetXenoPlaytime(player);
        if (time > _xenoPostfixTwoTime)
            return 2;
        else if (time > _xenoPostfixTime)
            return 1;

        return 0;
    }

    private void TransferName(EntityUid oldXeno, EntityUid newXeno)
    {
        if (_net.IsClient)
            return;

        if (!TryComp(oldXeno, out XenoNameComponent? oldName))
            return;

        var newName = EnsureComp<XenoNameComponent>(newXeno);
        newName.Rank = oldName.Rank;
        newName.Prefix = oldName.Prefix;
        newName.Number = oldName.Number;
        newName.Postfix = oldName.Postfix;
        Dirty(newXeno, newName);
        RemComp<AssignXenoNameComponent>(newXeno);

        _nameModifier.RefreshNameModifiers(newXeno);
    }

    public virtual void SetupName(EntityUid xeno)
    {
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<AssignXenoNameComponent>();
        while (query.MoveNext(out var uid, out _))
        {
            SetupName(uid);
        }
    }
}
