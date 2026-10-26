using Content.Shared.Damage;
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

    private static readonly ProtoId<TagPrototype> RMCKatanaTag = "RMCKatana";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<OmaeWaMouShindeiruComponent, MeleeHitEvent>(OnYouAreAlreadyDeadMeleeHit);
        SubscribeLocalEvent<OmaeWaMouShindeiruComponent, ComponentShutdown>(OnKatanaShutdown);
        SubscribeLocalEvent<OmaeTargetComponent, ComponentShutdown>(OnKatanaTargetShutdown);
    }

    private void OnYouAreAlreadyDeadMeleeHit(Entity<OmaeWaMouShindeiruComponent> ent, ref MeleeHitEvent args)
    {
        if (!args.IsHit)
            return;

        foreach (var target in args.HitEntities)
        {
            if (!ent.Comp.PendingTargets.Add(target))
                continue;

            if (_net.IsClient)
                continue;

            var damage = _melee.GetDamage(ent.Owner, args.User);
            if (HasKatana(target) && args.User != target)
            {
                // TODO RMC14 make the target swing at the user to imitate sword clashing.
                _stun.TryStun(args.User, ent.Comp.KillDelay, true);
                _stun.TryStun(target, ent.Comp.KillDelay, true);

                _popup.PopupEntity(Loc.GetString("rmc-katana-cross-blades-user"), args.User, args.User);
                _popup.PopupEntity(Loc.GetString("rmc-katana-cross-blades-target"), target, target);
            }

            if (ent.Comp.CanStunOnHit)
                _stun.TryStun(target, ent.Comp.KillDelay, true);

            OmaeWaMouShindeiru(ent, args.User, target, damage);
        }

        if (!ent.Comp.DamageOnHit)
        {
            if (TryComp(ent.Owner, out MeleeWeaponComponent? meleeWeapon))
                _audio.PlayPvs(meleeWeapon.HitSound ?? meleeWeapon.SwingSound, args.User);

            // Raise a MeleeAttackEvent so animation/effects listeners still run.
            var attackEv = new MeleeAttackEvent(ent.Owner);
            RaiseLocalEvent(args.User, ref attackEv);

            args.Handled = true;
        }
    }

    private void OmaeWaMouShindeiru(Entity<OmaeWaMouShindeiruComponent> ent, EntityUid user, EntityUid target, DamageSpecifier damage)
    {
        if (_net.IsClient)
            return;

        if (!ent.Comp.PendingTargets.Add(target))
            return;

        var targetComp = EnsureComp<OmaeTargetComponent>(target);
        targetComp.Attacker = user;
        targetComp.Damage = damage;
        targetComp.NumberOfCuts = ent.Comp.NumberOfCuts;
        targetComp.BypassResistances = ent.Comp.BypassResistances;

        var playStream = _audio.PlayLocal(ent.Comp.DeathImminentSound, target, target);
        if (playStream != null)
            targetComp.AudioStream = playStream.Value.Entity;

        Timer.Spawn(ent.Comp.KillDelay,
            () =>
            {
                ent.Comp.PendingTargets.Remove(target);

                if (targetComp.AudioStream != null)
                {
                    _audio.Stop(targetComp.AudioStream.Value);
                    targetComp.AudioStream = null;
                }

                RemCompDeferred<OmaeTargetComponent>(target);

                if (Deleted(target) || !Exists(target))
                    return;

                if (TryComp<MobStateComponent>(target, out var mobState) &&
                    mobState.CurrentState == MobState.Dead)
                {
                    return;
                }

                _audio.PlayPvs(ent.Comp.BoneBreakSound, target);
                for (var i = 0; i < targetComp.NumberOfCuts; i++)
                {
                    // TODO RMC14 hit outer limbs so this can decap/delimb
                    // var/def_zone = pick("head","l_leg","l_foot","r_leg","r_foot","l_arm","l_hand","r_arm","r_hand")
                    _damageable.TryChangeDamage(target, targetComp.Damage, ignoreResistances: targetComp.BypassResistances, origin: user, tool: ent.Owner);
                }
            });
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

    private void OnKatanaShutdown(EntityUid uid, OmaeWaMouShindeiruComponent comp, ComponentShutdown args)
    {
        comp.PendingTargets.Clear();
    }

    private void OnKatanaTargetShutdown(EntityUid uid, OmaeTargetComponent comp, ComponentShutdown args)
    {
        if (comp.AudioStream != null)
            _audio.Stop(comp.AudioStream.Value);
    }
}
