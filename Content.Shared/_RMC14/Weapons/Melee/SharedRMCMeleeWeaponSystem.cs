using Content.Shared._RMC14.Xenonids;
using Content.Shared.Damage;
using Content.Shared.Interaction.Events;
using Content.Shared.Stunnable;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Whitelist;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Weapons.Melee;

public abstract class SharedRMCMeleeWeaponSystem : EntitySystem
{
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly SharedMeleeWeaponSystem _melee = default!;

    private EntityQuery<MeleeWeaponComponent> _meleeWeaponQuery;
    private EntityQuery<XenoComponent> _xenoQuery;

    public override void Initialize()
    {
        _meleeWeaponQuery = GetEntityQuery<MeleeWeaponComponent>();
        _xenoQuery = GetEntityQuery<XenoComponent>();

        SubscribeLocalEvent<ImmuneToUnarmedComponent, GettingAttackedAttemptEvent>(OnImmuneToUnarmedGettingAttacked);

        SubscribeLocalEvent<ImmuneToMeleeComponent, GettingAttackedAttemptEvent>(OnImmuneToMeleeGettingAttacked);

        SubscribeLocalEvent<MeleeReceivedMultiplierComponent, DamageModifyEvent>(OnMeleeReceivedMultiplierDamageModify);

        SubscribeLocalEvent<StunOnHitComponent, MeleeHitEvent>(OnStunOnHitMeleeHit);

        SubscribeLocalEvent<MeleeDamageMultiplierComponent, MeleeHitEvent>(OnMultiplierOnHitMeleeHit);

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

        foreach (var hit in args.HitEntities)
        {
            if (_whitelist.IsValid(comp.Whitelist, hit))
            {
                var damage = args.BaseDamage * comp.Multiplier;
                args.BonusDamage += damage;
                break;
            }
        }
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


    private void TryMeleeReset(EntityUid weaponUid, MeleeWeaponComponent weapon, bool disarm){
        if (!TryComp<MeleeResetComponent>(weaponUid, out var reset))
            return;
        
        if (disarm)
            weapon.NextAttack = reset.OriginalTime;

        RemComp<MeleeResetComponent>(weaponUid);
        Dirty(weaponUid, weapon);
    }
}
