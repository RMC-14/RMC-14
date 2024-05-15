using Content.Server.Chat.Managers;
using Content.Server.GameTicking;
using Content.Server.Popups;
using Content.Shared._CM14.Xenos;
using Content.Shared._CM14.Xenos.Hive;
using Content.Shared.Chat;
using Content.Shared.Popups;
using Robust.Server.Audio;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server._CM14.Xenos.Hive;

public sealed class XenoHiveSystem : SharedXenoHiveSystem
{
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly IChatManager _chat = default!;
    [Dependency] private readonly IComponentFactory _compFactory = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private readonly List<string> _announce = [];

    public override void Initialize()
    {
        SubscribeLocalEvent<HiveComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<HiveComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.AnnouncedUnlocks.Clear();
        ent.Comp.Unlocks.Clear();
        ent.Comp.AnnouncementsLeft.Clear();

        foreach (var prototype in _prototypes.EnumeratePrototypes<EntityPrototype>())
        {
            if (prototype.TryGetComponent(out XenoComponent? xeno, _compFactory))
            {
                if (xeno.UnlockAt == default)
                    continue;

                ent.Comp.Unlocks.GetOrNew(xeno.UnlockAt).Add(prototype.ID);

                if (!ent.Comp.AnnouncementsLeft.Contains(xeno.UnlockAt))
                    ent.Comp.AnnouncementsLeft.Add(xeno.UnlockAt);
            }
        }

        foreach (var unlock in ent.Comp.Unlocks)
        {
            unlock.Value.Sort();
        }

        ent.Comp.AnnouncementsLeft.Sort();
    }

    public override void Update(float frameTime)
    {
        if (_gameTicker.RunLevel != GameRunLevel.InRound)
            return;

        var roundTime = _timing.CurTime - _gameTicker.RoundStartTimeSpan;
        var hives = EntityQueryEnumerator<HiveComponent>();
        while (hives.MoveNext(out var hiveId, out var hive))
        {
            _announce.Clear();

            for (var i = 0; i < hive.AnnouncementsLeft.Count; i++)
            {
                var left = hive.AnnouncementsLeft[i];
                if (roundTime >= left)
                {
                    if (hive.Unlocks.TryGetValue(left, out var unlocks))
                    {
                        foreach (var unlock in unlocks)
                        {
                            hive.AnnouncedUnlocks.Add(unlock);

                            if (_prototypes.TryIndex(unlock, out var prototype))
                            {
                                _announce.Add(prototype.Name);
                            }
                        }
                    }

                    hive.AnnouncementsLeft.RemoveAt(i);
                    i--;
                    Dirty(hiveId, hive);
                }
            }

            if (_announce.Count == 0)
                continue;

            var xenos = EntityQueryEnumerator<XenoComponent>();
            var popup = $"The Hive can now support: {string.Join(", ", _announce)}";
            var filter = Filter.Empty();
            while (xenos.MoveNext(out var xenoId, out var xeno))
            {
                if (xeno.Hive != hiveId)
                    continue;

                _popup.PopupEntity(popup, xenoId, xenoId, PopupType.Large);

                if (TryComp(xenoId, out ActorComponent? actor))
                    filter.AddPlayer(actor.PlayerSession);
            }

            var message = $"[color=#921992][font size=16][bold]{popup}[/bold][/font][/color]\n";
            _chat.ChatMessageToManyFiltered(filter, ChatChannel.Radio, popup, message, default, false, true, null);
            _audio.PlayGlobal(hive.AnnounceSound, filter, true);
        }
    }
}
