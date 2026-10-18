using Content.Shared._RMC14.Damage;
using Content.Shared.Explosion;
using Content.Shared.Popups;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Shields;

public sealed class VanguardShieldSystem : EntitySystem
{
    [Dependency] private readonly XenoShieldSystem _shield = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<VanguardShieldComponent, MapInitEvent>(OnVanguardShieldInit);
        SubscribeLocalEvent<VanguardShieldComponent, MeleeHitEvent>(OnVanguardShieldMelee);
        SubscribeLocalEvent<VanguardShieldComponent, DamageModifyAfterResistEvent>(OnVanguardShieldHit, before: [typeof(XenoShieldSystem)]);
        SubscribeLocalEvent<VanguardShieldComponent, GetExplosionResistanceEvent>(OnVanguardShieldGetResistance);
        SubscribeLocalEvent<VanguardShieldComponent, RemovedShieldEvent>(OnVanguardShieldRemoved);
    }

    private void OnVanguardShieldInit(Entity<VanguardShieldComponent> xeno, ref MapInitEvent args)
    {
        RegenShield(xeno);
    }
    private void OnVanguardShieldMelee(Entity<VanguardShieldComponent> xeno, ref MeleeHitEvent args)
    {
        if (args.HitEntities.Count <= 0 || !_net.IsServer)
            return;

        xeno.Comp.LastTimeHit = _timing.CurTime;
    }

    private void OnVanguardShieldHit(Entity<VanguardShieldComponent> xeno, ref DamageModifyAfterResistEvent args)
    {
        if (_net.IsServer)
            xeno.Comp.LastTimeHit = _timing.CurTime;

        if (args.Damage.GetTotal() <= 0 || !_shield.TryGetShield(xeno.Owner, XenoShieldSystem.ShieldType.Vanguard, out var _))
            return;

        if (!xeno.Comp.WasHit && args.Damage.GetTotal() > xeno.Comp.DecayThreshold)
        {
            xeno.Comp.WasHit = true;
            _popup.PopupEntity(Loc.GetString("rmc-xeno-shield-vanguard-hit"), xeno, xeno, PopupType.MediumCaution);

            args.Damage.ClampMax(0);
        }
    }

    private void OnVanguardShieldGetResistance(Entity<VanguardShieldComponent> xeno, ref GetExplosionResistanceEvent args)
    {
        if (!_shield.TryGetShield(xeno.Owner, XenoShieldSystem.ShieldType.Vanguard, out var shield))
            return;

        var explosionResist = xeno.Comp.ExplosionResistance;

        var resist = (float)Math.Pow(1.1, explosionResist / 5.0); // From armor calcualtion
        args.DamageCoefficient /= resist;
    }

    private void OnVanguardShieldRemoved(Entity<VanguardShieldComponent> xeno, ref RemovedShieldEvent args)
    {
        if (args.Type != XenoShieldSystem.ShieldType.Vanguard)
            return;

        _popup.PopupEntity(Loc.GetString("rmc-xeno-shield-vanguard-break"), xeno, xeno, PopupType.MediumCaution);
        xeno.Comp.NextDecay = _timing.CurTime;
    }


    public override void Update(float frameTime)
    {
        if (_net.IsClient)
            return;

        var time = _timing.CurTime;

        var vanguardQuery = EntityQueryEnumerator<VanguardShieldComponent, XenoShieldComponent>();

        while (vanguardQuery.MoveNext(out var uid, out var vanguardShield, out var shield))
        {
            if (vanguardShield.LastRecharge <= vanguardShield.LastTimeHit && vanguardShield.LastTimeHit + vanguardShield.RechargeTime <= time)
                RegenShield(uid);

            if (!_shield.TryGetShield((uid, shield), XenoShieldSystem.ShieldType.Vanguard, out var xenoShield) || !vanguardShield.WasHit || vanguardShield.NextDecay > time)
                continue;

            vanguardShield.NextDecay = time + vanguardShield.DecayEvery;

            xenoShield.ShieldAmount = Math.Max(0, ((xenoShield.ShieldAmount * vanguardShield.DecayMult) - vanguardShield.DecaySub).Double());
            _shield.UpdateVisuals((uid, shield));

            if (xenoShield.ShieldAmount <= 0)
                _shield.RemoveShield(uid, XenoShieldSystem.ShieldType.Vanguard);

            Dirty(uid, shield);
        }
    }

    //Returns whether something applies for the shield buff (for cleave)
    public bool ShieldBuff(EntityUid ent)
    {
        if (_shield.TryGetShield(ent, XenoShieldSystem.ShieldType.Vanguard, out _))
            return true;

        if (TryComp<VanguardShieldComponent>(ent, out var vanguard) && vanguard.NextDecay + vanguard.BuffExtraTime > _timing.CurTime)
            return true;

        return false;
    }

    public void RegenShield(EntityUid ent)
    {
        if (!TryComp<VanguardShieldComponent>(ent, out var vanguard))
            return;

        vanguard.LastRecharge = _timing.CurTime;
        vanguard.WasHit = false;

        if (!_shield.TryGetShield(ent, XenoShieldSystem.ShieldType.Vanguard, out _) && _net.IsServer)
            _popup.PopupEntity(Loc.GetString("rmc-xeno-shield-vanguard-regen"), ent, ent, PopupType.Medium);

        _shield.ApplyShield(ent, XenoShieldSystem.ShieldType.Vanguard, vanguard.RegenAmount, maxShield: vanguard.RegenAmount.Double());

    }
}
