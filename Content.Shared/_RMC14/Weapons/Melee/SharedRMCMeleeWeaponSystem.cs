using System.Linq;
using System.Numerics;
using Content.Shared._RMC14.CCVar;
using Content.Shared._RMC14.Marines.Skills;
using Content.Shared._RMC14.Xenonids;
using Content.Shared.ActionBlocker;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Interaction.Events;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Stunnable;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Whitelist;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Weapons.Melee;

public abstract class SharedRMCMeleeWeaponSystem : EntitySystem
{
    [Dependency] private readonly ActionBlockerSystem _blocker = default!;
    [Dependency] private readonly SharedMeleeWeaponSystem _melee = default!;
    [Dependency] private readonly INetConfigurationManager _netConfig = default!;
    [Dependency] private readonly SkillsSystem _skills = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly ItemToggleSystem _itemToggle = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    private EntityQuery<MeleeWeaponComponent> _meleeWeaponQuery;
    private EntityQuery<XenoComponent> _xenoQuery;

    public override void Initialize()
    {
        _meleeWeaponQuery = GetEntityQuery<MeleeWeaponComponent>();
        _xenoQuery = GetEntityQuery<XenoComponent>();

        SubscribeLocalEvent<ActorComponent, AttackAttemptEvent>(OnActorAttackAttempt);

        SubscribeLocalEvent<ImmuneToUnarmedComponent, GettingAttackedAttemptEvent>(OnImmuneToUnarmedGettingAttacked);

        SubscribeLocalEvent<ImmuneToMeleeComponent, GettingAttackedAttemptEvent>(OnImmuneToMeleeGettingAttacked);

        SubscribeLocalEvent<MeleeReceivedMultiplierComponent, DamageModifyEvent>(OnMeleeReceivedMultiplierDamageModify);

        SubscribeLocalEvent<StunOnHitComponent, MeleeHitEvent>(OnStunOnHitMeleeHit);

        SubscribeLocalEvent<MeleeDamageMultiplierComponent, MeleeHitEvent>(OnMultiplierOnHitMeleeHit);
        SubscribeLocalEvent<RMCMeleeDamageSkillComponent, MeleeHitEvent>(OnSkilledOnHitMeleeHit);

        SubscribeAllEvent<LightAttackEvent>(OnLightAttack, before: new[] { typeof(SharedMeleeWeaponSystem) });

        SubscribeAllEvent<HeavyAttackEvent>(OnHeavyAttack, before: new[] { typeof(SharedMeleeWeaponSystem) });

        SubscribeAllEvent<DisarmAttackEvent>(OnDisarmAttack, before: new[] { typeof(SharedMeleeWeaponSystem) });
    }

    //Call this whenever you add MeleeResetComponent to anything
    public void MeleeResetInit(Entity<MeleeResetComponent> ent)
    {
        if (!TryComp<MeleeWeaponComponent>(ent, out var weapon))
        {
            RemComp<MeleeResetComponent>(ent);
            return;
        }

        ent.Comp.OriginalTime = weapon.NextAttack;
        weapon.NextAttack = _timing.CurTime;
        Dirty(ent, weapon);
        Dirty(ent, ent.Comp);
    }

    private void OnStunOnHitMeleeHit(Entity<StunOnHitComponent> ent, ref MeleeHitEvent args)
    {
        if (!args.IsHit)
            return;

        if (!_itemToggle.IsActivated(ent.Owner))
            return;

        foreach (var hit in args.HitEntities)
        {
            if (_whitelist.IsValid(ent.Comp.Whitelist, hit))
                _stun.TryParalyze(hit, ent.Comp.Duration, true);
        }
    }

    private void OnMultiplierOnHitMeleeHit(Entity<MeleeDamageMultiplierComponent> ent, ref MeleeHitEvent args)
    {
        if (!args.IsHit)
            return;

        var comp = ent.Comp;

        args.BonusDamage = _skills.ApplyMeleeSkillModifier(args.User, args.BonusDamage);
        var totalDamage = args.BaseDamage + args.BonusDamage;

        foreach (var hit in args.HitEntities)
        {
            if (_whitelist.IsValid(comp.Whitelist, hit))
            {
                var damage = totalDamage * comp.Multiplier;
                args.BonusDamage += damage;
                break;
            }
        }
    }

    private void OnSkilledOnHitMeleeHit(Entity<RMCMeleeDamageSkillComponent> ent, ref MeleeHitEvent args)
    {
        if (!args.IsHit)
            return;

        if (!_prototypeManager.TryIndex<DamageGroupPrototype>(ent.Comp.BonusDamageType, out var bonusType))
            return;

        var totalBonusDamage = new DamageSpecifier(bonusType, _skills.GetSkill(ent.Owner, ent.Comp.Skill));
        args.BonusDamage += totalBonusDamage;
    }

    private void OnActorAttackAttempt(Entity<ActorComponent> ent, ref AttackAttemptEvent args)
    {
        if (args.Uid != args.Target)
            return;

        if (_netConfig.GetClientCVar(ent.Comp.PlayerSession.Channel, RMCCVars.RMCDamageYourself))
            return;

        args.Cancel();
    }

    private void OnImmuneToUnarmedGettingAttacked(Entity<ImmuneToUnarmedComponent> ent, ref GettingAttackedAttemptEvent args)
    {
        if (!ent.Comp.ApplyToXenos && _xenoQuery.HasComp(args.Attacker))
            return;

        if (args.Attacker == args.Weapon)
            args.Cancelled = true;
    }

    private void OnImmuneToMeleeGettingAttacked(Entity<ImmuneToMeleeComponent> ent, ref GettingAttackedAttemptEvent args)
    {
        if (_whitelist.IsWhitelistPassOrNull(ent.Comp.Whitelist, args.Attacker))
            args.Cancelled = true;
    }

    private void OnMeleeReceivedMultiplierDamageModify(Entity<MeleeReceivedMultiplierComponent> ent, ref DamageModifyEvent args)
    {
        if (!_meleeWeaponQuery.HasComp(args.Tool))
            return;

        if (_xenoQuery.HasComp(args.Origin))
        {
            args.Damage = new DamageSpecifier(ent.Comp.XenoDamage);
            return;
        }

        args.Damage = args.Damage * ent.Comp.OtherMultiplier;
    }

    private void OnLightAttack(LightAttackEvent msg, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not {} user)
            return;

        if (!_melee.TryGetWeapon(user, out var weaponUid, out var weapon) ||
            weaponUid != GetEntity(msg.Weapon))
        {
            return;
        }

        TryMeleeReset(weaponUid, weapon, false);
    }

    private void OnHeavyAttack(HeavyAttackEvent msg, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not {} user)
            return;

        if (!_melee.TryGetWeapon(user, out var weaponUid, out var weapon) ||
            weaponUid != GetEntity(msg.Weapon))
        {
            return;
        }

        TryMeleeReset(weaponUid, weapon, false);
    }

    private void OnDisarmAttack(DisarmAttackEvent msg, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not {} user)
            return;

        if (!_melee.TryGetWeapon(user, out var weaponUid, out var weapon))
        {
            return;
        }

        TryMeleeReset(weaponUid, weapon, true);
    }

    private void TryMeleeReset(EntityUid weaponUid, MeleeWeaponComponent weapon, bool disarm)
    {
        if (!TryComp<MeleeResetComponent>(weaponUid, out var reset))
            return;

        if (disarm)
            weapon.NextAttack = reset.OriginalTime;

        RemComp<MeleeResetComponent>(weaponUid);
        Dirty(weaponUid, weapon);
    }

    public void DoLunge(EntityUid user, EntityUid target)
    {
        var userXform = Transform(user);
        var targetPos = _transform.GetWorldPosition(target);
        var localPos = Vector2.Transform(targetPos, _transform.GetInvWorldMatrix(userXform));
        localPos = userXform.LocalRotation.RotateVec(localPos);
        _melee.DoLunge(user, target, Angle.Zero, localPos, null);
    }

    /// <summary>
    ///     Check if the attack event should be modified in any way.
    /// </summary>
    /// <param name="target">The initial target of the attack</param>
    /// <param name="weapon">The weapon used to attack</param>
    /// <param name="user">The entity doing the attack</param>
    /// <param name="attack">The <see cref="AttackEvent"/></param>
    /// <param name="newAttack">The new <see cref="AttackEvent"/></param>
    /// <param name="range">The range of the attack</param>
    /// <returns>True if the attack hasn't been modified, or if it is modified and still valid</returns>
    public bool AttemptOverrideAttack(EntityUid target, Entity<MeleeWeaponComponent> weapon, EntityUid user, AttackEvent attack, out AttackEvent newAttack, float range = 1.5f)
    {
        var targetPosition = _transform.GetMoverCoordinates(target).Position;
        var userPosition = _transform.GetMoverCoordinates(user).Position;
        var entities = GetNetEntityList(_melee.ArcRayCast(userPosition,
                (targetPosition -
                 userPosition).ToWorldAngle(),
                0,
                range,
                _transform.GetMapId(user),
                user)
            .ToList());

        var meleeEv = new MeleeAttackAttemptEvent(GetNetEntity(target),
            attack,
            attack.Coordinates,
            entities,
            GetNetEntity(weapon));
        RaiseLocalEvent(user, ref meleeEv);

        newAttack = meleeEv.Attack;

        // The attack hasn't been modified.
        if (attack == newAttack)
            return true;

        // The new target is the weapon being used for the attack.
        if (meleeEv.Weapon == meleeEv.Target)
            return false;

        var disarm = newAttack switch
        {
            DisarmAttackEvent => true,
            _ => false,
        };

        // The new target is unable to be attacked by the user.
        if (!_blocker.CanAttack(user, GetEntity(meleeEv.Target), weapon, disarm))
            return false;

        return true;
    }

    public float GetUserLightAttackRange(EntityUid user, EntityUid? target, MeleeWeaponComponent melee)
    {
        var ev = new RMCMeleeUserGetRangeEvent(target, melee.Range);
        RaiseLocalEvent(user, ref ev);
        return ev.Range;
    }
}
