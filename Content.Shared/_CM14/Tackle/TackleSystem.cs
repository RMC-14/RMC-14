using Content.Shared.CombatMode;
using Content.Shared.Damage.Systems;
using Content.Shared.Effects;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.IdentityManagement;
using Content.Shared.Popups;
using Content.Shared.Stunnable;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Shared._CM14.Tackle;

public sealed class TackleSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedColorFlashEffectSystem _colorFlash = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
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

        var time = _timing.CurTime;
        var recently = EnsureComp<TackledRecentlyComponent>(target);
        recently.LastTackled = time;
        recently.Current += tackle.Strength;

        Dirty(target, recently);

        _colorFlash.RaiseEffect(Color.Aqua, new List<EntityUid> { target.Owner }, Filter.PvsExcept(args.User));

        if (recently.Current < target.Comp.Threshold)
        {
            var targetName = Identity.Name(target, EntityManager, args.User);
            _popup.PopupClient($"You try to tackle {targetName}", args.User, args.User);

            foreach (var session in Filter.PvsExcept(args.User).Recipients)
            {
                if (session.AttachedEntity is not { } recipient)
                    continue;

                var userName = Identity.Name(args.User, EntityManager, recipient);
                targetName = Identity.Name(target, EntityManager, recipient);

                if (recipient == target.Owner)
                    _popup.PopupEntity($"{userName} tries to tackle you", args.User, recipient, PopupType.MediumCaution);
                else
                    _popup.PopupEntity($"{userName} tries to tackle {targetName}", args.User, recipient);
            }

            return;
        }
        else
        {
            var targetName = Identity.Name(target, EntityManager, args.User);
            _popup.PopupClient($"You tackle down {targetName}!", args.User, args.User);

            foreach (var session in Filter.PvsExcept(args.User).Recipients)
            {
                if (session.AttachedEntity is not { } recipient)
                    continue;

                var userName = Identity.Name(args.User, EntityManager, recipient);
                targetName = Identity.Name(target, EntityManager, recipient);

                if (recipient == target.Owner)
                    _popup.PopupEntity($"{userName} tackled you down!", args.User, recipient, PopupType.MediumCaution);
                else
                    _popup.PopupEntity($"{userName} tackles down {targetName}!", args.User, recipient);
            }
        }

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
        while (query.MoveNext(out var uid, out var recently, out var able))
        {
            if (time > recently.LastTackled + able.Expires)
                RemCompDeferred<TackledRecentlyComponent>(uid);
        }
    }
}
