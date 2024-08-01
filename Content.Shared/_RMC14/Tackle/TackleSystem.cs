using Content.Shared.CombatMode;
using Content.Shared.Damage.Systems;
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
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedColorFlashEffectSystem _colorFlash = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<TackleableComponent, CMDisarmEvent>(OnDisarmed, before: [typeof(SharedHandsSystem), typeof(StaminaSystem)]);
    }

    private void OnDisarmed(Entity<TackleableComponent> target, ref CMDisarmEvent args)
    {
        if (!TryComp(args.User, out TackleComponent? tackle))
            return;

        args.Handled = true;

        _colorFlash.RaiseEffect(Color.Aqua, new List<EntityUid> { target.Owner }, Filter.PvsExcept(args.User));

        var time = _timing.CurTime;
        var recently = EnsureComp<TackledRecentlyComponent>(target);
        recently.LastTackled = time;
        recently.LastTackledDuration = target.Comp.ExpireAfter;
        recently.Current += tackle.Strength;

        Dirty(target, recently);

        if (recently.Current < tackle.Threshold)
        {
            _popup.PopupClient(Loc.GetString("cm-tackle-try-self", ("target", target.Owner)), args.User, args.User);

            foreach (var session in Filter.PvsExcept(args.User).Recipients)
            {
                if (session.AttachedEntity is not { } recipient)
                    continue;

                if (recipient == target.Owner)
                    _popup.PopupEntity(Loc.GetString("cm-tackle-try-target", ("user", args.User)), args.User, recipient, PopupType.MediumCaution);
                else
                    _popup.PopupEntity(Loc.GetString("cm-tackle-try-observer", ("user", args.User), ("target", target.Owner)), args.User, recipient);
            }

            return;
        }
        else
        {
            _popup.PopupClient(Loc.GetString("cm-tackle-success-self", ("target", target.Owner)), args.User, args.User);

            foreach (var session in Filter.PvsExcept(args.User).Recipients)
            {
                if (session.AttachedEntity is not { } recipient)
                    continue;

                if (recipient == target.Owner)
                    _popup.PopupEntity(Loc.GetString("cm-tackle-success-target", ("user", args.User)), args.User, recipient, PopupType.MediumCaution);
                else
                    _popup.PopupEntity(Loc.GetString("cm-tackle-success-observer", ("user", args.User), ("target", target.Owner)), args.User, recipient);
            }
        }

        if (_net.IsClient)
            return;

        if (TryComp(args.User, out CombatModeComponent? combatMode))
        {
            var audioParams = AudioParams.Default.WithVariation(0.025f).WithVolume(5f);
            _audio.PlayPredicted(combatMode.DisarmSuccessSound, target, args.User, audioParams);
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
