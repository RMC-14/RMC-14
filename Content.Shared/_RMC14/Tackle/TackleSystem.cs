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
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

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
        var recently = EnsureComp<TackledRecentlyComponent>(target);
        recently.LastTackled = time;
        recently.LastTackledDuration = target.Comp.ExpireAfter;
        recently.Current += tackle.Strength;

        Dirty(target, recently);

        if (recently.Current < tackle.Threshold)
        {
            _adminLog.Add(LogType.RMCTackle, $"{ToPrettyString(user)} tried to tackle {ToPrettyString(target)}.");
            _popup.PopupClient(Loc.GetString("cm-tackle-try-self", ("target", target.Owner)), user, user);

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
            _popup.PopupClient(Loc.GetString("cm-tackle-success-self", ("target", target.Owner)), user, user);

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

        if (_net.IsClient)
            return;

        if (TryComp(user, out CombatModeComponent? combatMode))
        {
            var audioParams = AudioParams.Default.WithVariation(0.025f).WithVolume(5f);
            _audio.PlayPredicted(combatMode.DisarmSuccessSound, target, user, audioParams);
        }

        _stun.TryParalyze(target, tackle.Stun, true);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var time = _timing.CurTime;
        var query = EntityQueryEnumerator<TackledRecentlyComponent, TackleableComponent>();
        while (query.MoveNext(out var uid, out var recently, out _))
        {
            if (time > recently.LastTackled + recently.LastTackledDuration)
                RemCompDeferred<TackledRecentlyComponent>(uid);
        }
    }
}
