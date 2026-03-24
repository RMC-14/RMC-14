using System.Numerics;
using Content.Shared._RMC14.Areas;
using Content.Shared._RMC14.BlurredVision;
using Content.Shared._RMC14.Chat;
using Content.Shared._RMC14.Deafness;
using Content.Shared._RMC14.Marines;
using Content.Shared._RMC14.Pulling;
using Content.Shared._RMC14.Slow;
using Content.Shared._RMC14.Stamina;
using Content.Shared._RMC14.Stun;
using Content.Shared._RMC14.Synth;
using Content.Shared._RMC14.Xenonids.Construction.Nest;
using Content.Shared._RMC14.Xenonids.Parasite;
using Content.Shared.ActionBlocker;
using Content.Shared.Chat;
using Content.Shared.Coordinates;
using Content.Shared.Damage;
using Content.Shared.Drugs;
using Content.Shared.Drunk;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Jittering;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Projectiles;
using Content.Shared.Random.Helpers;
using Content.Shared.Rejuvenate;
using Content.Shared.Speech.EntitySystems;
using Content.Shared.StatusEffect;
using Content.Shared.Stunnable;
using Content.Shared.Throwing;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Xenonids.Neurotoxin;

public abstract class SharedNeurotoxinSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly RMCStaminaSystem _stamina = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;
    [Dependency] private readonly SharedSlurredSystem _slurred = default!;
    [Dependency] private readonly SharedStutteringSystem _stutter = default!;
    [Dependency] private readonly RMCDazedSystem _daze = default!;
    [Dependency] private readonly SharedJitteringSystem _jitter = default!;
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!; //It's how this fakes movement
    [Dependency] private readonly ActionBlockerSystem _blocker = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly RMCPullingSystem _rmcPulling = default!;
    [Dependency] private readonly RMCSlowSystem _slow = default!;
    [Dependency] private readonly SharedDeafnessSystem _deafness = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedCMChatSystem _rmcChat = default!;
    [Dependency] private readonly ISharedPlayerManager _player = default!;
    [Dependency] private readonly AreaSystem _area = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    private readonly HashSet<Entity<MarineComponent>> _marines = new();
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<NeurotoxinComponent, RejuvenateEvent>(OnRejuvenate);
        SubscribeLocalEvent<NeurotoxinInjectorComponent, ProjectileHitEvent>(OnProjectileHit);
    }

    private void OnRejuvenate(Entity<NeurotoxinComponent> ent, ref RejuvenateEvent args)
    {
        RemCompDeferred<NeurotoxinComponent>(ent);
    }

    private void OnProjectileHit(Entity<NeurotoxinInjectorComponent> ent, ref ProjectileHitEvent args)
    {
        if (!HasComp<MarineComponent>(args.Target))
            return;

        if (!ent.Comp.AffectsDead && _mobState.IsDead(args.Target))
            return;

        if (!ent.Comp.AffectsInfectedNested &&
                    HasComp<XenoNestedComponent>(args.Target) &&
                    HasComp<VictimInfectedComponent>(args.Target))
        {
            return;
        }

        var time = _timing.CurTime;

        if (!EnsureComp<NeurotoxinComponent>(args.Target, out var neuro))
        {
            neuro.LastMessage = time;
            neuro.LastAccentTime = time;
            neuro.LastStumbleTime = time;
        }

        _statusEffects.TryAddStatusEffect<RMCBlindedComponent>(args.Target, "Blinded", neuro.BlurTime * 6, true);
        _daze.TryDaze(ent, ent.Comp.DazeTime, true, stutter: true);
        neuro.NeurotoxinAmount += ent.Comp.NeuroPerSecond;
        neuro.ToxinDamage = ent.Comp.ToxinDamage;
        neuro.OxygenDamage = ent.Comp.OxygenDamage;
        neuro.CoughDamage = ent.Comp.CoughDamage;
    }


    public override void Update(float frameTime)
    {
        if (_net.IsClient)
            return;

        var time = _timing.CurTime;
        var neurotoxinInjectorQuery = EntityQueryEnumerator<NeurotoxinInjectorComponent>();

        while (neurotoxinInjectorQuery.MoveNext(out var uid, out var neuroGas))
        {
            if (!neuroGas.InjectInContact)
                continue;

            _marines.Clear();
            _entityLookup.GetEntitiesInRange(uid.ToCoordinates(), 0.5f, _marines);

            foreach (var marine in _marines)
            {
                if (!neuroGas.AffectsDead && _mobState.IsDead(marine))
                    continue;

                if (!neuroGas.AffectsInfectedNested &&
                    HasComp<XenoNestedComponent>(marine) &&
                    HasComp<VictimInfectedComponent>(marine))
                {
                    continue;
                }

                var ev = new NeurotoxinInjectAttemptEvent();
                RaiseLocalEvent(marine, ref ev);

                if (ev.Cancelled)
                    continue;

                if (!EnsureComp<NeurotoxinComponent>(marine, out var builtNeurotoxin))
                {
                    builtNeurotoxin.LastMessage = time;
                    builtNeurotoxin.LastAccentTime = time;
                    builtNeurotoxin.LastStumbleTime = time;
                    builtNeurotoxin.NextGasInjectionAt = time;
                    builtNeurotoxin.NextNeuroEffectAt = time;
                }

                if (time < builtNeurotoxin.NextGasInjectionAt)
                    continue;

                _statusEffects.TryAddStatusEffect<RMCBlindedComponent>(marine, "Blinded", builtNeurotoxin.BlurTime * 12, true);
                _daze.TryDaze(marine, neuroGas.DazeTime, true, stutter: true);
                builtNeurotoxin.NeurotoxinAmount += neuroGas.NeuroPerSecond;
                builtNeurotoxin.ToxinDamage = neuroGas.ToxinDamage;
                builtNeurotoxin.OxygenDamage = neuroGas.OxygenDamage;
                builtNeurotoxin.CoughDamage = neuroGas.CoughDamage;
                builtNeurotoxin.NextGasInjectionAt = time + neuroGas.TimeBetweenGasInjects;
            }
        }

        var neuroToxinQuery = EntityQueryEnumerator<NeurotoxinComponent>();

        while (neuroToxinQuery.MoveNext(out var uid, out var neuro))
        {
            if (time < neuro.NextNeuroEffectAt)
                continue;

            neuro.NeurotoxinAmount -= neuro.DepletionPerTick;

            neuro.NextNeuroEffectAt = time + neuro.UpdateEvery;

            if (neuro.NeurotoxinAmount <= 0 || HasComp<SynthComponent>(uid))
            {
                RemCompDeferred<NeurotoxinComponent>(uid);
                continue;
            }

            if (_mobState.IsDead(uid))
                continue;

            //Basic Effects
            _stamina.DoStaminaDamage(uid, neuro.StaminaDamagePerTick, visual: false);
            _statusEffects.TryAddStatusEffect<DrunkComponent>(uid, "Drunk", neuro.DizzyStrength, true);

            NeurotoxinNonStackingEffects(uid, neuro, time, out var coughChance, out var stumbleChance);
            NeurotoxinStackingEffects(uid, neuro, time);

            if (_random.Prob(stumbleChance) && time - neuro.LastStumbleTime >= neuro.MinimumDelayBetweenEvents)
            {
                neuro.LastStumbleTime = time;
                // This is how we randomly move them - by throwing
                if (_blocker.CanMove(uid))
                {
                    _rmcPulling.TryStopPullsOn(uid);
                    _physics.SetLinearVelocity(uid, Vector2.Zero);
                    _physics.SetAngularVelocity(uid, 0f);
                    _throwing.TryThrow(uid, _random.NextAngle().ToVec().Normalized() / 10, 10, animated: false, playSound: false, doSpin: false, compensateFriction: true);
                }
                _popup.PopupEntity(Loc.GetString("rmc-stumble-others", ("victim", uid)), uid, Filter.PvsExcept(uid), true, PopupType.SmallCaution);
                _popup.PopupEntity(Loc.GetString("rmc-stumble"), uid, uid, PopupType.MediumCaution);
                _daze.TryDaze(uid, neuro.DazeLength * 5, true, stutter: true);
                _jitter.DoJitter(uid, neuro.StumbleJitterTime, true);
                _statusEffects.TryAddStatusEffect<DrunkComponent>(uid, "Drunk", neuro.DizzyStrengthOnStumble, true);
                var ev = new NeurotoxinEmoteEvent() { Emote = neuro.PainId };
                RaiseLocalEvent(uid, ev);
            }

            if (_random.Prob(coughChance))
            {
                _slow.TrySlowdown(uid, neuro.BloodCoughDuration);
                _damage.TryChangeDamage(uid, neuro.CoughDamage); // TODO RMC-14 specifically chest damage
                _popup.PopupEntity(Loc.GetString("rmc-bloodcough"), uid, uid, PopupType.MediumCaution);
                var ev = new NeurotoxinEmoteEvent() { Emote = neuro.CoughId };
                RaiseLocalEvent(uid, ev);
            }

        }

        var neuroHallucinationQuery = EntityQueryEnumerator<NeurotoxinLingeringHallucinationComponent>();

        while (neuroHallucinationQuery.MoveNext(out var uid, out var hallu))
        {
            if (hallu.Hallucinations.Count == 0)
            {
                RemCompDeferred<NeurotoxinLingeringHallucinationComponent>(uid);
                continue;
            }

            List<(NeuroHallucinations, int, TimeSpan, EntityCoordinates?)> toRemove = new();
            List<(NeuroHallucinations, int, TimeSpan, EntityCoordinates?)> toAdd = new();

            foreach (var entry in hallu.Hallucinations)
            {
                if (entry.Item3 > time)
                    continue;

                var newEntry = ProcessHallucination(uid, hallu, entry);

                toRemove.Add(entry);

                if (newEntry != null)
                    toAdd.Add(newEntry.Value);
            }

            hallu.Hallucinations.RemoveAll(a => toRemove.Contains(a));

            hallu.Hallucinations.AddRange(toAdd);
        }

    }

    private void NeurotoxinNonStackingEffects(EntityUid victim, NeurotoxinComponent neurotoxin, TimeSpan time, out float coughChance, out float stumbleChance)
    {
        string message = "rmc-neuro-tired";
        PopupType poptype = PopupType.Small;
        coughChance = 0;
        stumbleChance = 0;
        if (neurotoxin.NeurotoxinAmount <= 9)
        {
            //Do nothing, the intial conditions are already set
        }
        else if (neurotoxin.NeurotoxinAmount <= 14)
        {
            message = "rmc-neuro-numb";
            poptype = PopupType.SmallCaution;
            coughChance = 0.10f;
        }
        else if (neurotoxin.NeurotoxinAmount <= 19)
        {
            int chance = _random.Next(4);
            if (chance == 0)
            {
                message = "rmc-neuro-where";
                poptype = PopupType.Large;
            }
            else
            {
                message = _random.Pick(new List<string> {"rmc-neuro-very-numb", "rmc-neuro-erratic", "rmc-neuro-panic"});
                poptype = PopupType.MediumCaution;
            }
            coughChance = 0.10f;
            stumbleChance = 0.05f;
        }
        else if (neurotoxin.NeurotoxinAmount <= 24)
        {
            message = "rmc-neuro-sting";
            poptype = PopupType.MediumCaution;
            coughChance = 0.25f;
            stumbleChance = 0.25f;

        }
        else
        {
            int chance = _random.Next(7);
            if (chance == 0)
            {
                message = "rmc-neuro-what";
                poptype = PopupType.Large;
            }
            else if (chance == 1)
            {
                message = "rmc-neuro-hearing";
                poptype = PopupType.MediumCaution;
            }
            else
            {
                message = _random.Pick(new List<string> { "rmc-neuro-pain", "rmc-neuro-agh", "rmc-neuro-so-numb", "rmc-neuro-limbs", "rmc-neuro-think"});
                poptype = PopupType.LargeCaution;
            }
            coughChance = 0.25f;
            stumbleChance = 0.25f;
        }

        if (time - neurotoxin.LastMessage >= neurotoxin.TimeBetweenMessages)
        {
            neurotoxin.LastMessage = time;
            _popup.PopupEntity(Loc.GetString(message), victim, victim, poptype);
        }
    }

    private void NeurotoxinStackingEffects(EntityUid victim, NeurotoxinComponent neurotoxin, TimeSpan currTime)
    {
        if (neurotoxin.NeurotoxinAmount >= 10)
        {
            _statusEffects.TryAddStatusEffect<RMCBlindedComponent>(victim, "Blinded", neurotoxin.BlurTime, true);
            if (currTime - neurotoxin.LastAccentTime >= neurotoxin.MinimumDelayBetweenEvents)
            {
                neurotoxin.LastAccentTime = currTime;
                if (_random.Prob(0.5f))
                    _slurred.DoSlur(victim, neurotoxin.AccentTime);
                else
                    _stutter.DoStutter(victim, neurotoxin.AccentTime, true);
            }
        }

        if (neurotoxin.NeurotoxinAmount >= 15)
        {
            // TODO RMC14 Agony effect - gives fake damage, pain needs this too so maybe then
            _jitter.DoJitter(victim, neurotoxin.JitterTime, true);
            if (currTime >= neurotoxin.NextHallucination)
            {
                neurotoxin.NextHallucination = currTime + _random.Next(neurotoxin.HallucinationEveryMin, neurotoxin.HallucinationEveryMax);
                DoNeuroHallucination(victim, neurotoxin);
            }
        }

        if (neurotoxin.NeurotoxinAmount >= 20)
        {
            _statusEffects.TryAddStatusEffect<TemporaryBlindnessComponent>(victim, "TemporaryBlindness", neurotoxin.BlindTime, true);
        }

        if (neurotoxin.NeurotoxinAmount >= 27)
        {
            _daze.TryDaze(victim, neurotoxin.DazeLength, true, stutter: true);
            _damage.TryChangeDamage(victim, neurotoxin.ToxinDamage);
            _deafness.TryDeafen(victim, neurotoxin.DeafenTime, true, ignoreProtection: true);
        }

        if (neurotoxin.NeurotoxinAmount >= 50)
        {
            // TODO RMC14 also gives liver damage
            _damage.TryChangeDamage(victim, neurotoxin.OxygenDamage);
        }
    }

    private void DoNeuroHallucination(EntityUid victim, NeurotoxinComponent neurotoxin)
    {
        var hallucination = SharedRandomExtensions.Pick(neurotoxin.Hallucinations, _random.GetRandom());
        //Note event times are hardcoded for now since thers alot of them
        switch (hallucination)
        {
            case NeuroHallucinations.AlienAttack:
                _audio.PlayStatic(neurotoxin.Pounce, victim, victim.ToCoordinates());
                _stun.TryParalyze(victim, neurotoxin.PounceDownTime, true);
                var lingering = EnsureComp<NeurotoxinLingeringHallucinationComponent>(victim);
                lingering.Hallucinations.Add((NeuroHallucinations.AlienAttack, 0, _timing.CurTime + TimeSpan.FromSeconds(1), null));
                break;
            case NeuroHallucinations.OB:
                //Little extra to confuse the player
                //TODO RMC14 replace if it gets a locId
                if (_player.TryGetSessionByEntity(victim, out var session))
                {
                    var msg = "[font size=16][color=red]Orbital bombardment launch command detected![/color][/font]";
                    msg = $"[bold][font size=24][color=red]\n{msg}\n[/color][/font][/bold]";
                    _rmcChat.ChatMessageToOne(ChatChannel.Radio, msg, msg, default, false, session.Channel, recordReplay: true);

                    if (_area.TryGetArea(victim.ToCoordinates(), out _, out var areaProto))
                    {
                        var warhead = _random.Pick(neurotoxin.WarheadTypes);

                        if (_proto.TryIndex(warhead, out var warHeadProto))
                        {
                            msg = $"[color=red]Launch command informs {warHeadProto.Name}. Estimated impact area: {areaProto.Name}[/color]";
                            _rmcChat.ChatMessageToOne(ChatChannel.Radio, msg, msg, default, false, session.Channel, recordReplay: true);
                        }
                    }
                }
                _audio.PlayGlobal(neurotoxin.OBAlert, victim);
                lingering = EnsureComp<NeurotoxinLingeringHallucinationComponent>(victim);
                lingering.Hallucinations.Add((NeuroHallucinations.OB, 0, _timing.CurTime + TimeSpan.FromSeconds(2), null));
                break;
            case NeuroHallucinations.Screech:
                _audio.PlayStatic(neurotoxin.Screech, victim, HallucinationSoundOffset(victim, 3));
                _stun.TryParalyze(victim, neurotoxin.ScreechDownTime, true);
                break;
            case NeuroHallucinations.CAS:
                var position = HallucinationSoundOffset(victim, 7);
                _audio.PlayStatic(neurotoxin.FiremissionStart, victim, position);
                lingering = EnsureComp<NeurotoxinLingeringHallucinationComponent>(victim);
                lingering.Hallucinations.Add((NeuroHallucinations.CAS, 0, _timing.CurTime + TimeSpan.FromSeconds(3.5), position));
                break;
            case NeuroHallucinations.Giggle:
                var ev = new NeurotoxinEmoteEvent() { Emote = neurotoxin.GiggleId };
                RaiseLocalEvent(victim, ev);
                //TODO RMC14 hallucination status - more in depth than neuro
                _statusEffects.TryAddStatusEffect<SeeingRainbowsStatusEffectComponent>(victim, "StatusEffectSeeingRainbow", neurotoxin.RainbowDuration, true);
                break;
            case NeuroHallucinations.Mortar:
                position = HallucinationSoundOffset(victim, 7);
                FakeWarning(position, victim, "rmc-mortar-shell-impact-warning", "rmc-mortar-shell-impact-warning-above");
                lingering = EnsureComp<NeurotoxinLingeringHallucinationComponent>(victim);
                lingering.Hallucinations.Add((NeuroHallucinations.Mortar, 0, _timing.CurTime + TimeSpan.FromSeconds(1), position));
                break;
            case NeuroHallucinations.Sounds:
                var sound = _random.Pick(neurotoxin.HallucinationRandomSounds);
                //Random offset to make it spookier if it's real or not
                _audio.PlayStatic(sound, victim, HallucinationSoundOffset(victim, 7));
                break;
        }
    }

    //Returns true if the hallucination is done.
    private (NeuroHallucinations, int, TimeSpan, EntityCoordinates?)? ProcessHallucination(EntityUid victim, NeurotoxinLingeringHallucinationComponent lingering, (NeuroHallucinations, int, TimeSpan, EntityCoordinates?) hallucination)
    {
        switch (hallucination.Item1)
        {
            case NeuroHallucinations.AlienAttack:
                if (hallucination.Item2 == 0)
                {
                    _audio.PlayStatic(lingering.XenoClaw, victim, victim.ToCoordinates());
                    _audio.PlayStatic(lingering.BoneBreak, victim, victim.ToCoordinates());
                    hallucination.Item2 = 1;
                    hallucination.Item3 = _timing.CurTime + TimeSpan.FromSeconds(0.5);
                    return hallucination;
                }
                else if (hallucination.Item2 < 3)
                {
                    _audio.PlayStatic(lingering.XenoClaw, victim, victim.ToCoordinates());
                    hallucination.Item2 += 1;
                    hallucination.Item3 = _timing.CurTime + TimeSpan.FromSeconds(0.5);
                    return hallucination;
                }
                else
                {
                    _audio.PlayStatic(lingering.BoneBreak, victim, victim.ToCoordinates());
                    // TODO RMC14 Agony
                    var ev = new NeurotoxinEmoteEvent() { Emote = lingering.PainEmote };
                    RaiseLocalEvent(victim, ev);
                }
                break;

            case NeuroHallucinations.OB:
                _audio.PlayStatic(lingering.OBTravel, victim, HallucinationSoundOffset(victim, 7));
                break;

            case NeuroHallucinations.CAS: //Very long unfortunately
                if (hallucination.Item2 == 0)
                {
                    FakeWarning(hallucination.Item4 ?? victim.ToCoordinates(), victim, "rmc-dropship-firemission-warning", "rmc-dropship-firemission-warning-above");
                    hallucination.Item2 = 1;
                    hallucination.Item3 = _timing.CurTime + TimeSpan.FromSeconds(0.5);
                    return hallucination;
                }
                else if (hallucination.Item2 == 1)
                {
                    _audio.PlayStatic(lingering.RocketFire, victim, HallucinationSoundOffset(hallucination.Item4 ?? victim.ToCoordinates(), 7));
                    hallucination.Item2 = 2;
                    hallucination.Item3 = _timing.CurTime + TimeSpan.FromSeconds(0.5);
                    return hallucination;
                }
                else if (hallucination.Item2 == 2)
                {
                    _audio.PlayStatic(lingering.GauFire, victim, HallucinationSoundOffset(hallucination.Item4 ?? victim.ToCoordinates(), 7));
                    hallucination.Item2 = 3;
                    hallucination.Item3 = _timing.CurTime + TimeSpan.FromSeconds(0.5);
                    return hallucination;
                }
                else if (hallucination.Item2 == 3)
                {
                    _audio.PlayStatic(lingering.RocketFire, victim, HallucinationSoundOffset(hallucination.Item4 ?? victim.ToCoordinates(), 7));
                    _audio.PlayStatic(lingering.GauHit, victim, HallucinationSoundOffset(hallucination.Item4 ?? victim.ToCoordinates(), 7));
                    _audio.PlayStatic(lingering.GauHit, victim, HallucinationSoundOffset(hallucination.Item4 ?? victim.ToCoordinates(), 7));
                    hallucination.Item2 = 4;
                    hallucination.Item3 = _timing.CurTime + TimeSpan.FromSeconds(1);
                    return hallucination;
                }
                else if (hallucination.Item2 == 4)
                {
                    _audio.PlayStatic(lingering.Explosion, victim, HallucinationSoundOffset(hallucination.Item4 ?? victim.ToCoordinates(), 7));
                    _audio.PlayStatic(lingering.GauHit, victim, HallucinationSoundOffset(hallucination.Item4 ?? victim.ToCoordinates(), 7));
                    hallucination.Item2 = 5;
                    hallucination.Item3 = _timing.CurTime + TimeSpan.FromSeconds(1);
                    return hallucination;
                }
                else if (hallucination.Item2 == 5)
                {
                    _audio.PlayStatic(lingering.RocketFire, victim, HallucinationSoundOffset(hallucination.Item4 ?? victim.ToCoordinates(), 7));
                    _audio.PlayStatic(lingering.GauHit, victim, HallucinationSoundOffset(hallucination.Item4 ?? victim.ToCoordinates(), 7));
                    hallucination.Item2 = 6;
                    hallucination.Item3 = _timing.CurTime + TimeSpan.FromSeconds(1);
                    return hallucination;
                }
                else if (hallucination.Item2 == 6)
                {
                    _audio.PlayStatic(lingering.Explosion, victim, HallucinationSoundOffset(hallucination.Item4 ?? victim.ToCoordinates(), 7));
                    _audio.PlayStatic(lingering.GauHit, victim, HallucinationSoundOffset(hallucination.Item4 ?? victim.ToCoordinates(), 7));
                    _audio.PlayStatic(lingering.GauHit, victim, HallucinationSoundOffset(hallucination.Item4 ?? victim.ToCoordinates(), 7));
                    hallucination.Item2 = 7;
                    hallucination.Item3 = _timing.CurTime + TimeSpan.FromSeconds(0.5);
                    return hallucination;
                }
                else if (hallucination.Item2 == 7)
                {
                    _audio.PlayStatic(lingering.BigExplosion, victim, HallucinationSoundOffset(hallucination.Item4 ?? victim.ToCoordinates(), 7));
                    _audio.PlayStatic(lingering.GauHit, victim, HallucinationSoundOffset(hallucination.Item4 ?? victim.ToCoordinates(), 7));
                    hallucination.Item2 = 8;
                    hallucination.Item3 = _timing.CurTime + TimeSpan.FromSeconds(0.5);
                    return hallucination;
                }
                else if (hallucination.Item2 == 8)
                {
                    _audio.PlayStatic(lingering.RocketFire, victim, HallucinationSoundOffset(hallucination.Item4 ?? victim.ToCoordinates(), 7));
                    hallucination.Item2 = 9;
                    hallucination.Item3 = _timing.CurTime + TimeSpan.FromSeconds(0.5);
                    return hallucination;
                }
                else if (hallucination.Item2 == 9)
                {
                    _audio.PlayStatic(lingering.GauHit, victim, HallucinationSoundOffset(hallucination.Item4 ?? victim.ToCoordinates(), 7));
                    _audio.PlayStatic(lingering.Explosion, victim, HallucinationSoundOffset(hallucination.Item4 ?? victim.ToCoordinates(), 7));
                    hallucination.Item2 = 10;
                    hallucination.Item3 = _timing.CurTime + TimeSpan.FromSeconds(0.5);
                    return hallucination;
                }
                else if (hallucination.Item2 == 10)
                {
                    _audio.PlayStatic(lingering.GauHit, victim, HallucinationSoundOffset(hallucination.Item4 ?? victim.ToCoordinates(), 7));
                    hallucination.Item2 = 11;
                    hallucination.Item3 = _timing.CurTime + TimeSpan.FromSeconds(0.5);
                    return hallucination;
                }
                else
                {
                    _audio.PlayStatic(lingering.Explosion, victim, HallucinationSoundOffset(hallucination.Item4 ?? victim.ToCoordinates(), 7));
                    _audio.PlayStatic(lingering.GauHit, victim, HallucinationSoundOffset(hallucination.Item4 ?? victim.ToCoordinates(), 7));
                    var ev = new NeurotoxinEmoteEvent() { Emote = lingering.PainEmote };
                    RaiseLocalEvent(victim, ev);
                }
                break;

            case NeuroHallucinations.Mortar:
                _audio.PlayStatic(lingering.MortarTravel, victim, hallucination.Item4 ?? victim.ToCoordinates());
                break;
        }
        return null;
    }

    private EntityCoordinates HallucinationSoundOffset(EntityUid victim, float maxDistance)
    {
        var randomOffset =
        new Vector2
        (
            _random.NextFloat(-maxDistance, maxDistance + 0.01f),
            _random.NextFloat(-maxDistance, maxDistance + 0.01f)
        );

        var newCoords = Transform(victim).Coordinates.Offset(randomOffset);

        return newCoords;
    }

    private EntityCoordinates HallucinationSoundOffset(EntityCoordinates coords, float maxDistance)
    {
        var randomOffset =
        new Vector2
        (
            _random.NextFloat(-maxDistance, maxDistance + 0.01f),
            _random.NextFloat(-maxDistance, maxDistance + 0.01f)
        );

        var newCoords = coords.Offset(randomOffset);

        return newCoords;
    }

    private void FakeWarning(EntityCoordinates coords, EntityUid player, LocId directionWarning, LocId aboveWarning)
    {
        var distanceVec = _transform.GetMapCoordinates(player).Position - _transform.ToMapCoordinates(coords).Position;
        var distance = distanceVec.Length();

        var direction = distanceVec.GetDir().ToString().ToUpperInvariant();

        var msg = distance < 1
        ? Loc.GetString(aboveWarning)
        : Loc.GetString(directionWarning, ("direction", direction));

        _popup.PopupEntity(msg, player, player, PopupType.LargeCaution);

        if (_player.TryGetSessionByEntity(player, out var session))
        {
            msg = $"[bold][font size=24][color=red]\n{msg}\n[/color][/font][/bold]";
            _rmcChat.ChatMessageToOne(ChatChannel.Radio, msg, msg, default, false, session.Channel, recordReplay: true);
        }
    }
}
