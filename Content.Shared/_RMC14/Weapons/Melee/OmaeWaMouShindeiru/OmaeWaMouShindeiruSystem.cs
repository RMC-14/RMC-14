using System.Numerics;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Content.Shared.Stunnable;
using Content.Shared.Tag;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Weapons.Melee.OmaeWaMouShindeiru;

public sealed class OmaeWaMouShindeiruSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedMeleeWeaponSystem _melee = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private static readonly ProtoId<DamageTypePrototype> SlashType = "Slash";
    private static readonly ProtoId<TagPrototype> RMCKatanaTag = "RMCKatana";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<OmaeWaMouShindeiruComponent, MeleeHitEvent>(OnYouAreAlreadyDeadMeleeHit, after: [typeof(SharedRMCMeleeWeaponSystem)]);
    }

    private void OnYouAreAlreadyDeadMeleeHit(Entity<OmaeWaMouShindeiruComponent> ent, ref MeleeHitEvent args)
    {
        if (!args.IsHit)
            return;

        var damage = args.BaseDamage + args.BonusDamage;
        foreach (var target in args.HitEntities)
        {
            if (!_timing.IsFirstTimePredicted)
                continue;

            if (TryComp<MobStateComponent>(target, out var mobState) &&
                mobState.CurrentState == MobState.Dead)
            {
                continue;
            }

            if (HasKatana(target) && args.User != target)
            {
                CounterSwing(args.User, target);

                _stun.TryStun(args.User, ent.Comp.KillDelay, true);
                _stun.TryStun(target, ent.Comp.KillDelay, true);

                _popup.PopupPredicted(Loc.GetString("rmc-katana-cross-blades-user", ("target", MetaData(target).EntityName)), args.User, null, PopupType.Medium);
                _popup.PopupPredicted(Loc.GetString("rmc-katana-cross-blades-target", ("user", MetaData(args.User).EntityName)), target, null, PopupType.Medium);
            }

            if (ent.Comp.CanStunOnKatanaClash)
                _stun.TryStun(target, ent.Comp.KillDelay, true);

            OmaeWaMouShindeiru(ent, args.User, target, damage);
        }

        if (!ent.Comp.DamageOnHit)
        {
            var chipDamage = new DamageSpecifier { DamageDict = { [SlashType.Id] = 1 } };
            args.BonusDamage += -damage + chipDamage;
            args.ModifiersList.Clear();
        }
    }

    private void OmaeWaMouShindeiru(Entity<OmaeWaMouShindeiruComponent> ent, EntityUid user, EntityUid target, DamageSpecifier damage)
    {
        if (_net.IsClient)
            return;

        if (HasComp<AlreadyDeadTargetComponent>(target))
            return;

        var delayedComp = EnsureComp<AlreadyDeadTargetComponent>(target);
        delayedComp.TriggerTime = _timing.CurTime + ent.Comp.KillDelay;
        delayedComp.NumberOfCuts = ent.Comp.NumberOfCuts;
        delayedComp.BypassResistances = ent.Comp.BypassResistances;
        delayedComp.Damage = damage;
        delayedComp.User = user;

        if (delayedComp.AudioStream is { } oldStream && Exists(oldStream))
            _audio.Stop(oldStream);

        var playStream = _audio.PlayPvs(ent.Comp.DeathImminentSound, target);
        delayedComp.AudioStream = playStream?.Entity;
    }

    private void CounterSwing(EntityUid user, EntityUid target)
    {
        if (_net.IsServer)
            return;

        EntityUid? katanaUid = null;
        string? animation = null;
        foreach (var held in _hands.EnumerateHeld(target))
        {
            if (_tag.HasTag(held, RMCKatanaTag) && TryComp(held, out MeleeWeaponComponent? katana))
            {
                katanaUid = held;
                animation = katana.WideAnimation;
                break;
            }
        }

        var targetXform = Transform(target);
        var userPos = _transform.GetWorldPosition(user);
        var localPos = Vector2.Transform(userPos, _transform.GetInvWorldMatrix(targetXform));
        localPos = targetXform.LocalRotation.RotateVec(localPos);
        _melee.DoLunge(target, katanaUid ?? target, Angle.Zero, localPos, animation);
    }

    private bool HasKatana(EntityUid target)
    {
        foreach (var held in _hands.EnumerateHeld(target))
        {
            if (_tag.HasTag(held, RMCKatanaTag))
                return true;
        }

        return false;
    }

    public override void Update(float frameTime)
    {
        if (_net.IsClient)
            return;

        var curTime = _timing.CurTime;
        var query = EntityQueryEnumerator<AlreadyDeadTargetComponent>();
        while (query.MoveNext(out var uid, out var targetComp))
        {
            if (targetComp.TriggerTime > curTime)
                continue;

            if (targetComp.AudioStream is { } stream &&
                Exists(stream))
            {
                _audio.Stop(stream);
                targetComp.AudioStream = null;
            }

            if (TryComp<MobStateComponent>(uid, out var mobState) &&
                mobState.CurrentState == MobState.Dead)
            {
                RemComp<AlreadyDeadTargetComponent>(uid);
                continue;
            }

            _audio.PlayPvs(targetComp.BoneBreakSound, uid);
            for (var i = 0; i < targetComp.NumberOfCuts; i++)
            {
                // TODO RMC14 hit outer limbs so this can decap/delimb
                // var/def_zone = pick("head","l_leg","l_foot","r_leg","r_foot","l_arm","l_hand","r_arm","r_hand")
                _damageable.TryChangeDamage(uid, targetComp.Damage, ignoreResistances: targetComp.BypassResistances, origin: targetComp.User);
            }

            RemComp<AlreadyDeadTargetComponent>(uid);
        }
    }
}
