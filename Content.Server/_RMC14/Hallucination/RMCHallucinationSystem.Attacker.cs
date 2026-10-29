using System.Linq;
using System.Numerics;
using Content.Shared._RMC14.BlurredVision;
using Content.Shared._RMC14.Hallucination;
using Content.Shared._RMC14.Hallucination.Events;
using Content.Shared._RMC14.Stun;
using Content.Shared.Bed.Sleep;
using Content.Shared.Chat;
using Content.Shared.Humanoid;
using Content.Shared.Maps;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Movement.Components;
using Content.Shared.Popups;
using Content.Shared.StatusEffect;
using Content.Shared.Weapons.Melee;
using Robust.Shared.Audio;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server._RMC14.Hallucination;

public sealed partial class RMCHallucinationSystem
{
    private static readonly ProtoId<StatusEffectPrototype> BlurStatus = "Blinded";

    private static readonly TimeSpan AttackerProcessEvery = TimeSpan.FromSeconds(0.5);
    private static readonly TimeSpan AttackerLifetime = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan AttackerFade = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan AttackerSyncEvery = TimeSpan.FromSeconds(0.25);
    private static readonly TimeSpan FakeBloodLifetime = TimeSpan.FromSeconds(30);

    private const float AttackerSpeed = 4f / 3f;
    private const float AttackerCrossRange = 0.5f;
    private const float AttackerHitBackRange = 1.5f;
    private const float AttackerSpawnDistance = 10;
    private const float AttackerFootstepVolume = 1.5f;
    private const int AttackerLines = 10;

    private void InitializeAttacker()
    {
        SubscribeLocalEvent<RMCHallucinationFakeAttackerComponent, RMCHallucinateEvent>(OnFakeAttacker);

        SubscribeNetworkEvent<RMCHallucinationAttackerHitEvent>(OnAttackerHit);
    }

    private void OnFakeAttacker(Entity<RMCHallucinationFakeAttackerComponent> ent, ref RMCHallucinateEvent args)
    {
        args.Handled = FakeAttack(args.Victim);
    }

    private void OnAttackerHit(RMCHallucinationAttackerHitEvent msg, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not { } victim ||
            !TryComp(victim, out RMCHallucinatingComponent? hallucinating))
        {
            return;
        }

        var attacker = hallucinating.Attackers.FirstOrDefault(a => a.Id == msg.Id);
        if (attacker == null || attacker.RemoveAt != null)
            return;

        if (GetEntity(msg.Weapon) is not { Valid: true } weapon ||
            weapon == victim ||
            !_hands.IsHolding(victim, weapon) ||
            !TryComp(weapon, out MeleeWeaponComponent? melee))
        {
            return;
        }

        if (!TryGetLocalPosition(victim, attacker.Grid, out var victimPosition) ||
            Vector2.Distance(victimPosition, attacker.Position) > AttackerHitBackRange)
        {
            return;
        }

        var force = (float) _melee.GetDamage(weapon, victim, melee).GetTotal();
        attacker.Health -= force;

        BackOff(attacker, victimPosition);

        _popup.PopupEntity(Loc.GetString("rmc-hallucination-flails", ("user", victim)), victim, Filter.PvsExcept(victim), true, PopupType.SmallCaution);
        _audio.PlayGlobal(hallucinating.WeaponHitSound, victim);
        ShowMessage(victim, Loc.GetString("rmc-hallucination-attacker-attacked", ("attacker", attacker.Name), ("user", victim)));
    }

    private bool FakeAttack(Entity<RMCHallucinatingComponent> ent)
    {
        if (Transform(ent).GridUid is not { } grid || !TryGetLocalPosition(ent, grid, out var position))
            return false;

        var clones = new List<EntityUid>();
        var query = EntityQueryEnumerator<HumanoidAppearanceComponent, MobStateComponent>();
        while (query.MoveNext(out var uid, out _, out var mobState))
        {
            if (mobState.CurrentState != MobState.Alive ||
                HasComp<SleepingComponent>(uid) ||
                HasComp<RMCUnconsciousComponent>(uid) ||
                Transform(uid).MapID == MapId.Nullspace)
            {
                continue;
            }

            clones.Add(uid);
        }

        if (clones.Count == 0)
            return false;

        var clone = _random.Pick(clones);
        EntityUid? weapon = null;
        foreach (var held in _hands.EnumerateHeld(clone))
        {
            weapon = held;
            break;
        }

        EntProtoId? animation = null;
        if (TryComp(weapon, out MeleeWeaponComponent? weaponMelee))
            animation = weaponMelee.Animation;
        else if (weapon == null && TryComp(clone, out MeleeWeaponComponent? unarmed))
            animation = unarmed.Animation;

        SoundSpecifier? footstep = null;
        var hasShoes = false;
        if (TryComp(clone, out FootstepModifierComponent? cloneFootstep))
        {
            footstep = cloneFootstep.FootstepSoundCollection;
        }
        else if (_inventory.TryGetSlotEntity(clone, "shoes", out var shoes))
        {
            hasShoes = true;
            if (TryComp(shoes, out FootstepModifierComponent? shoeFootstep))
                footstep = shoeFootstep.FootstepSoundCollection;
        }

        var time = _timing.CurTime;
        var attacker = new RMCHallucinationAttacker
        {
            Id = ent.Comp.NextAttackerId++,
            Clone = clone,
            Name = Name(clone),
            Weapon = weapon == null ? null : Name(weapon.Value),
            WeaponEntity = weapon,
            Animation = animation,
            FootstepSound = footstep,
            HasShoes = hasShoes,
            Grid = grid,
            Position = position + _random.NextAngle().ToVec() * AttackerSpawnDistance,
            VictimInside = false,
            End = time + AttackerLifetime,
            NextProcess = time + AttackerProcessEvery,
            NextSync = time + AttackerSyncEvery,
            NextSpeak = time + RandomTime(ent.Comp.AttackerSpeakMin, ent.Comp.AttackerSpeakMax),
        };

        if (TryComp(clone, out MobMoverComponent? mobMover))
        {
            attacker.StepDistance = mobMover.StepSoundMoveDistanceWalking;
            attacker.FootstepVariation = mobMover.FootstepVariation;
        }

        ent.Comp.Attackers.Add(attacker);

        if (TryComp(ent, out ActorComponent? actor))
            _pvsOverride.AddSessionOverride(clone, actor.PlayerSession);

        SendToVictim(ent, new RMCHallucinationAttackerSpawnEvent(attacker.Id, GetNetEntity(clone), GetNetCoordinates(new EntityCoordinates(grid, attacker.Position))));

        return true;
    }

    private void MoveAttacker(Entity<RMCHallucinatingComponent> ent, RMCHallucinationAttacker attacker, float frameTime)
    {
        if (attacker.RemoveAt != null ||
            frameTime <= 0 ||
            !TryGetLocalPosition(ent, attacker.Grid, out var victim) ||
            !HasComp<MapGridComponent>(attacker.Grid))
        {
            return;
        }

        var time = _timing.CurTime;
        var direction = Vector2.Zero;
        if (attacker.BackOffUntil is { } backOffUntil && time < backOffUntil)
        {
            var away = attacker.Position - victim;
            if (away.LengthSquared() > 0.0001f)
                direction = Vector2.Normalize(away);
        }
        else
        {
            attacker.BackOffUntil = null;
            if (ChebyshevDistance(attacker.Position, victim) > 1)
                direction = Vector2.Normalize(victim - attacker.Position);
        }

        var velocity = direction * AttackerSpeed;
        attacker.Position += velocity * frameTime;

        if (velocity != Vector2.Zero)
        {
            attacker.Direction = MathF.Abs(velocity.X) > MathF.Abs(velocity.Y)
                ? velocity.X > 0 ? Direction.East : Direction.West
                : velocity.Y > 0 ? Direction.North : Direction.South;

            attacker.StepProgress += AttackerSpeed * frameTime;
            if (attacker.StepProgress >= attacker.StepDistance)
            {
                attacker.StepProgress -= attacker.StepDistance;
                AttackerFootstep(ent, attacker);
            }
        }

        var changed = Vector2.DistanceSquared(velocity, attacker.Velocity) > 0.01f;
        attacker.Velocity = velocity;
        if (!changed && (velocity == Vector2.Zero || time < attacker.NextSync))
            return;

        attacker.NextSync = time + AttackerSyncEvery;
        SendToVictim(ent, new RMCHallucinationAttackerMoveEvent(
            attacker.Id,
            GetNetCoordinates(new EntityCoordinates(attacker.Grid, attacker.Position)),
            velocity,
            attacker.Direction));
    }

    private void AttackerFootstep(Entity<RMCHallucinatingComponent> ent, RMCHallucinationAttacker attacker)
    {
        if (!TryComp(attacker.Grid, out MapGridComponent? grid))
            return;

        var coords = new EntityCoordinates(attacker.Grid, attacker.Position);
        var sound = attacker.FootstepSound;
        if (sound == null)
        {
            var indices = _map.TileIndicesFor(attacker.Grid, grid, coords);
            if (!_map.TryGetTileRef(attacker.Grid, grid, indices, out var tileRef))
                return;

            var tile = (ContentTileDefinition) _tileDefinitions[tileRef.Tile.TypeId];
            sound = attacker.HasShoes ? tile.FootstepSounds : tile.BarestepSounds;
        }

        if (sound == null)
            return;

        var audioParams = sound.Params
            .WithVolume(sound.Params.Volume + AttackerFootstepVolume)
            .WithVariation(sound.Params.Variation ?? attacker.FootstepVariation);
        _audio.PlayStatic(sound, ent, coords, audioParams);
    }

    private void ProcessAttacker(Entity<RMCHallucinatingComponent> ent, RMCHallucinationAttacker attacker)
    {
        var time = _timing.CurTime;
        if (attacker.RemoveAt is { } removeAt)
        {
            if (time >= removeAt)
                RemoveAttacker(ent, attacker);

            return;
        }

        if (time >= attacker.End || !TryGetLocalPosition(ent, attacker.Grid, out var victim))
        {
            RemoveAttacker(ent, attacker);
            return;
        }

        if (attacker.Health < 0)
        {
            attacker.RemoveAt = time + AttackerFade;
            SendToVictim(ent, new RMCHallucinationAttackerCollapseEvent(attacker.Id, AttackerFade));
            return;
        }

        if (time >= attacker.NextSpeak && Vector2.Distance(attacker.Position, victim) <= ent.Comp.AttackerSpeakRange)
        {
            attacker.NextSpeak = time + RandomTime(ent.Comp.AttackerSpeakMin, ent.Comp.AttackerSpeakMax);
            AttackerSay(ent, attacker);
        }

        var inside = Vector2.Distance(attacker.Position, victim) < AttackerCrossRange;
        if (inside && !attacker.VictimInside && _random.Prob(0.3f))
            _popup.PopupEntity(Loc.GetString("rmc-hallucination-stumbles", ("user", ent.Owner)), ent, Filter.PvsExcept(ent), true, PopupType.SmallCaution);

        attacker.VictimInside = inside;

        if (!attacker.SkippedLastTurn && _random.Prob(0.5f))
        {
            attacker.SkippedLastTurn = true;
            return;
        }

        attacker.SkippedLastTurn = false;

        if (ChebyshevDistance(attacker.Position, victim) > 1)
            return;

        if (_random.Prob(0.15f))
            AttackerHit(ent, attacker);

        if (_random.Prob(0.15f))
            BackOff(attacker, victim);
    }

    private void AttackerSay(Entity<RMCHallucinatingComponent> ent, RMCHallucinationAttacker attacker)
    {
        if (Deleted(attacker.Clone))
            return;

        var message = Loc.GetString($"rmc-hallucination-attacker-say-{_random.Next(1, AttackerLines + 1)}", ("victim", Name(ent)));
        var verb = _chat.GetSpeechVerb(attacker.Clone, message);
        var wrapped = Loc.GetString(verb.Bold ? "chat-manager-entity-say-bold-wrap-message" : "chat-manager-entity-say-wrap-message",
            ("entityName", FormattedMessage.EscapeText(attacker.Name)),
            ("verb", Loc.GetString(_random.Pick(verb.SpeechVerbStrings))),
            ("fontType", verb.FontId),
            ("fontSize", verb.FontSize),
            ("message", FormattedMessage.EscapeText(message)));

        SendToVictim(ent, new RMCHallucinationSpeechEvent(attacker.Id, false, ChatChannel.Local, message, wrapped));
    }

    private void AttackerHit(Entity<RMCHallucinatingComponent> ent, RMCHallucinationAttacker attacker)
    {
        var weapon = attacker.WeaponEntity is { } held && !TerminatingOrDeleted(held) ? GetNetEntity(held) : (NetEntity?) null;
        SendToVictim(ent, new RMCHallucinationAttackerSwingEvent(attacker.Id, weapon, attacker.Animation));

        if (attacker.Weapon != null)
        {
            _audio.PlayGlobal(ent.Comp.WeaponHitSound, ent);

            if (_random.Prob(0.2f))
                _oldStatus.TryAddStatusEffect<RMCBlindedComponent>(ent, BlurStatus, TimeSpan.FromSeconds(6), false);
        }
        else
        {
            _audio.PlayGlobal(ent.Comp.PunchSound, ent);
        }

        if (_random.Prob(0.33f))
            TryFakeBlood(ent, attacker.Grid);
    }

    private void TryFakeBlood(Entity<RMCHallucinatingComponent> ent, EntityUid grid)
    {
        if (!TryComp(grid, out MapGridComponent? gridComp) || Transform(ent).GridUid != grid)
            return;

        var tile = _map.TileIndicesFor(grid, gridComp, Transform(ent).Coordinates);
        var time = _timing.CurTime;
        if (ent.Comp.FakeBlood.TryGetValue((grid, tile), out var end) && time < end)
            return;

        ent.Comp.FakeBlood[(grid, tile)] = time + FakeBloodLifetime;
        var coords = _map.GridTileToLocal(grid, gridComp, tile);
        SendToVictim(ent, new RMCHallucinationVisualEvent(BloodVisual, $"mfloor{_random.Next(1, 8)}", GetNetCoordinates(coords), FakeBloodLifetime));
    }

    private void BackOff(RMCHallucinationAttacker attacker, Vector2 victim)
    {
        if (ChebyshevDistance(attacker.Position, victim) > 2)
            return;

        attacker.BackOffUntil = _timing.CurTime + TimeSpan.FromSeconds(1 / AttackerSpeed);
    }

    private void RemoveAttacker(Entity<RMCHallucinatingComponent> ent, RMCHallucinationAttacker attacker)
    {
        ent.Comp.Attackers.Remove(attacker);

        if (TryComp(ent, out ActorComponent? actor) &&
            !Deleted(attacker.Clone) &&
            ent.Comp.Attackers.All(a => a.Clone != attacker.Clone))
        {
            _pvsOverride.RemoveSessionOverride(attacker.Clone, actor.PlayerSession);
        }

        SendToVictim(ent, new RMCHallucinationAttackerRemoveEvent(attacker.Id));
    }
}
