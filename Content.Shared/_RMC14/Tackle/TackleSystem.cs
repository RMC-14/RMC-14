using Content.Shared._RMC14.Xenonids.Parasite;
using Content.Shared.Administration.Logs;
using Content.Shared.CombatMode;
using Content.Shared.Damage.Systems;
using Content.Shared.Database;
using Content.Shared.Effects;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Stunnable;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Tackle;

public sealed class TackleSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLog = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedColorFlashEffectSystem _colorFlash = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private readonly List<EntityUid> _trackersToRemove = new();

    public override void Initialize()
    {
        SubscribeLocalEvent<TackleableComponent, CMDisarmEvent>(OnDisarmed, before: [typeof(SharedHandsSystem), typeof(StaminaSystem)]);
    }

    private void OnDisarmed(Entity<TackleableComponent> target, ref CMDisarmEvent args)
    {
        var user = args.User;
        if (!TryComp(user, out TackleComponent? tackle))
            return;

        args.Handled = true;

        _colorFlash.RaiseEffect(Color.Aqua, new List<EntityUid> { target.Owner }, Filter.PvsExcept(user));

        var time = _timing.CurTime;
        var recently = EnsureComp<TackledRecentlyComponent>(user);
        var tracker = recently.Trackers.GetValueOrDefault(target);
        tracker.Count++;
        tracker.Last = time;

        recently.Trackers[target] = tracker;
        Dirty(user, recently);

        if (_net.IsClient)
            return;

        if (tracker.Count < tackle.Min ||
            tracker.Count < _random.Next(tackle.Min, tackle.Max + 1))
        {
            _adminLog.Add(LogType.RMCTackle, $"{ToPrettyString(user)} tried to tackle {ToPrettyString(target)}.");

            if (_net.IsServer)
                _popup.PopupEntity(Loc.GetString("cm-tackle-try-self", ("target", target.Owner)), user, user);

            foreach (var session in Filter.PvsExcept(user).Recipients)
            {
                if (session.AttachedEntity is not { } recipient)
                    continue;

                if (recipient == target.Owner)
                    _popup.PopupEntity(Loc.GetString("cm-tackle-try-target", ("user", user)), user, recipient, PopupType.MediumCaution);
                else
                    _popup.PopupEntity(Loc.GetString("cm-tackle-try-observer", ("user", user), ("target", target.Owner)), user, recipient);
            }

            return;
        }
        else
        {
            _adminLog.Add(LogType.RMCTackle, $"{ToPrettyString(user)} tackled down {ToPrettyString(target)}.");

            if (_net.IsServer)
                _popup.PopupEntity(Loc.GetString("cm-tackle-success-self", ("target", target.Owner)), user, user);

            foreach (var session in Filter.PvsExcept(user).Recipients)
            {
                if (session.AttachedEntity is not { } recipient)
                    continue;

                if (recipient == target.Owner)
                    _popup.PopupEntity(Loc.GetString("cm-tackle-success-target", ("user", user)), user, recipient, PopupType.MediumCaution);
                else
                    _popup.PopupEntity(Loc.GetString("cm-tackle-success-observer", ("user", user), ("target", target.Owner)), user, recipient);
            }
        }

        if (TryComp(user, out CombatModeComponent? combatMode))
        {
            var audioParams = AudioParams.Default.WithVariation(0.025f).WithVolume(5f);
            _audio.PlayPredicted(combatMode.DisarmSuccessSound, target, user, audioParams);
        }

        if (!HasComp<VictimInfectedComponent>(target))
            recently.Trackers.Remove(target);

        var stun = tackle.StunMin;
        if (tackle.StunMin < tackle.StunMax)
            stun = _random.Next(tackle.StunMin, tackle.StunMax);

        stun *= 2;
        _stun.TryParalyze(target, stun, true);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var time = _timing.CurTime;
        var query = EntityQueryEnumerator<TackledRecentlyComponent>();
        while (query.MoveNext(out var uid, out var recently))
        {
            _trackersToRemove.Clear();
            foreach (var tracker in recently.Trackers)
            {
                if (time >= tracker.Value.Last + recently.ExpireAfter)
                    _trackersToRemove.Add(tracker.Key);
            }

            foreach (var id in _trackersToRemove)
            {
                recently.Trackers.Remove(id);
            }

            if (recently.Trackers.Count == 0)
                RemCompDeferred<TackledRecentlyComponent>(uid);
        }
    }
}
