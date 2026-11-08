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
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private static readonly ProtoId<DamageTypePrototype> SlashType = "Slash";
    private static readonly ProtoId<TagPrototype> RMCKatanaTag = "RMCKatana";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<OmaeWaMouShindeiruComponent, MeleeHitEvent>(OnYouAreAlreadyDeadMeleeHit, after: [typeof(SharedRMCMeleeWeaponSystem)]);
        SubscribeLocalEvent<OmaeWaMouShindeiruComponent, ComponentShutdown>(OnKatanaShutdown);
    }

    private void OnYouAreAlreadyDeadMeleeHit(Entity<OmaeWaMouShindeiruComponent> ent, ref MeleeHitEvent args)
    {
        if (!args.IsHit)
            return;

        var damage = args.BaseDamage + args.BonusDamage;
        foreach (var target in args.HitEntities)
        {
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

                _popup.PopupEntity(Loc.GetString("rmc-katana-cross-blades-user", ("target", MetaData(target).EntityName)), args.User, PopupType.Medium);
                _popup.PopupEntity(Loc.GetString("rmc-katana-cross-blades-target", ("user", MetaData(args.User).EntityName)), target, PopupType.Medium);
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

        if (!ent.Comp.PendingTargets.Add(target))
            return;

        var playStream = _audio.PlayPvs(ent.Comp.DeathImminentSound, target);
        if (playStream != null)
            ent.Comp.Streams[target] = playStream.Value.Entity;

        var cuts = ent.Comp.NumberOfCuts;
        var ignoreRes = ent.Comp.BypassResistances;
        Timer.Spawn(ent.Comp.KillDelay,
            () =>
            {
                if (!Exists(ent.Owner))
                    return;

                ent.Comp.PendingTargets.Remove(target);

                if (ent.Comp.Streams.TryGetValue(target, out var stream))
                {
                    _audio.Stop(stream);
                    ent.Comp.Streams.Remove(target);
                }

                if (Deleted(target) || !Exists(target))
                    return;

                if (TryComp<MobStateComponent>(target, out var mobState) &&
                    mobState.CurrentState == MobState.Dead)
                {
                    return;
                }

                _audio.PlayPvs(ent.Comp.BoneBreakSound, target);
                for (var i = 0; i < cuts; i++)
                {
                    // TODO RMC14 hit outer limbs so this can decap/delimb
                    // var/def_zone = pick("head","l_leg","l_foot","r_leg","r_foot","l_arm","l_hand","r_arm","r_hand")
                    _damageable.TryChangeDamage(target, damage, ignoreResistances: ignoreRes, origin: user, tool: ent.Owner);
                }
            });
    }

    private void CounterSwing(EntityUid user, EntityUid target)
    {
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

    private void OnKatanaShutdown(EntityUid uid, OmaeWaMouShindeiruComponent comp, ComponentShutdown args)
    {
        foreach (var stream in comp.Streams.Values)
        {
            _audio.Stop(stream);
        }

        comp.Streams.Clear();
        comp.PendingTargets.Clear();
    }
}
