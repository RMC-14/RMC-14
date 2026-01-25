using Content.Shared._RMC14.Armor;
using Content.Shared._RMC14.Aura;
using Content.Shared._RMC14.Xenonids.Heal;
using Content.Shared.Examine;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Xenonids.Rage;

public sealed class XenoRageSystem : EntitySystem
{
    [Dependency] private readonly SharedMeleeWeaponSystem _meleeWeapon = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly XenoSystem _xeno = default!;
    [Dependency] private readonly SharedXenoHealSystem _xenoHeal = default!;
    [Dependency] private readonly SharedAuraSystem _aura = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeed = default!;
    [Dependency] private readonly CMArmorSystem _armor = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<XenoRageComponent, MeleeHitEvent>(OnRageMeleeHit);
        SubscribeLocalEvent<XenoRageComponent, RefreshMovementSpeedModifiersEvent>(OnRageRefreshSpeed);
        SubscribeLocalEvent<XenoRageComponent, GetMeleeAttackRateEvent>(OnRageGetMeleeAttackRate);
        SubscribeLocalEvent<XenoRageComponent, CMGetArmorEvent>(OnRageGetArmor);
        SubscribeLocalEvent<XenoRageComponent, ExaminedEvent>(OnRageExamine);
    }

    public void IncrementRage(Entity<XenoRageComponent?> xeno, int amount)
    {
        if (!Resolve(xeno, ref xeno.Comp, false))
            return;

        if (xeno.Comp.RageCooldownExpireAt > _timing.CurTime)
            return;

        if (xeno.Comp.RageLocked)
            return;

        xeno.Comp.Rage += amount;
        xeno.Comp.Rage = Math.Min(xeno.Comp.Rage, xeno.Comp.MaxRage);
        Dirty(xeno);

        _movementSpeed.RefreshMovementSpeedModifiers(xeno);
        _armor.UpdateArmorValue(xeno.Owner);

        if (xeno.Comp.Rage >= xeno.Comp.MaxRage)
            RageLock((xeno.Owner, xeno.Comp));
    }

    public int GetRage(EntityUid uid)
    {
        if (!TryComp<XenoRageComponent>(uid, out var rage))
            return 0;

        return rage.Rage;
    }

    public void RageLock(Entity<XenoRageComponent> xeno)
    {
        xeno.Comp.RageLocked = true;
        xeno.Comp.RageLockExpireAt = _timing.CurTime + xeno.Comp.RageLockDuration;
        Dirty(xeno);

        _movementSpeed.RefreshMovementSpeedModifiers(xeno);
        _armor.UpdateArmorValue(xeno.Owner);
        _aura.GiveAura(xeno, xeno.Comp.RageLockColor, xeno.Comp.RageLockDuration, 3);
        _popup.PopupClient(Loc.GetString("rmc-xeno-rage-lock"), xeno, xeno, PopupType.Medium);
    }

    public void RageUnlock(Entity<XenoRageComponent> xeno)
    {
        xeno.Comp.RageLocked = false;
        IncrementRage(xeno.Owner, -xeno.Comp.Rage);
        xeno.Comp.RageCooldownExpireAt = _timing.CurTime + xeno.Comp.RageCooldownDuration;
        Dirty(xeno);

        var msg = Loc.GetString("rmc-xeno-rage-expire", ("cooldown", xeno.Comp.RageCooldownDuration.Seconds.ToString()));
        _popup.PopupEntity(msg, xeno, xeno, PopupType.MediumCaution);
        _aura.GiveAura(xeno, xeno.Comp.RageLockWeakenColor, xeno.Comp.RageCooldownDuration);
    }

    private void OnRageMeleeHit(Entity<XenoRageComponent> xeno, ref MeleeHitEvent args)
    {
        if (!args.IsHit || args.HitEntities.Count == 0)
            return;

        var validTarget = false;
        foreach (var entity in args.HitEntities)
        {
            if (!_xeno.CanAbilityAttackTarget(xeno.Owner, entity))
                continue;

            validTarget = true;
            break;
        }

        if (!validTarget)
            return;

        IncrementRage(xeno.Owner, 1);

        var healAmount = (0.05 * xeno.Comp.Rage + 0.3) * xeno.Comp.HealAmount;
        _xenoHeal.CreateHealStacks(xeno, healAmount, xeno.Comp.RageHealTime, 1, xeno.Comp.RageHealTime);

        xeno.Comp.LastHit = _timing.CurTime;
        Dirty(xeno);
    }

    private void OnRageRefreshSpeed(Entity<XenoRageComponent> xeno, ref RefreshMovementSpeedModifiersEvent args)
    {
        var speed = 1 + xeno.Comp.Rage * xeno.Comp.SpeedBuffPerRage;
        args.ModifySpeed(speed, speed);
    }

    private void OnRageGetMeleeAttackRate(Entity<XenoRageComponent> xeno, ref GetMeleeAttackRateEvent args)
    {
        args.Rate += xeno.Comp.Rage * xeno.Comp.AttackSpeedPerRage;
    }

    private void OnRageGetArmor(Entity<XenoRageComponent> xeno, ref CMGetArmorEvent args)
    {
        args.XenoArmor += xeno.Comp.Rage * xeno.Comp.ArmorPerRage;
    }

    private void OnRageExamine(Entity<XenoRageComponent> xeno, ref ExaminedEvent args)
    {
        if (!HasComp<XenoComponent>(args.Examiner))
            return;

        using (args.PushGroup(nameof(XenoRageComponent)))
        {
            args.PushMarkup(Loc.GetString("rmc-xeno-rage-examine", ("xeno", xeno),
                ("amount", xeno.Comp.Rage), ("max", xeno.Comp.MaxRage)));
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_net.IsClient)
            return;

        var time = _timing.CurTime;
        var rageQuery = EntityQueryEnumerator<XenoRageComponent>();
        while (rageQuery.MoveNext(out var uid, out var rage))
        {
            if (rage.LastHit + rage.RageDecayTime <= time &&
                rage.Rage > 0 &&
                !rage.RageLocked)
            {
                IncrementRage((uid, rage), -1);
                rage.LastHit = time;
                Dirty(uid, rage);
            }

            if (!rage.RageLocked)
                continue;

            if (rage.RageLockExpireAt <= time)
                RageUnlock((uid, rage));
        }
    }
}
