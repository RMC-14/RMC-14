using Content.Shared._RMC14.Armor;
using Content.Shared._RMC14.Damage;
using Content.Shared.Damage;
using Content.Shared.Explosion;
using Content.Shared.Popups;
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
        SubscribeLocalEvent<VanguardShieldComponent, DamageModifyAfterResistEvent>(OnVanguardShieldHit, before: [typeof(XenoShieldSystem)]);
        SubscribeLocalEvent<VanguardShieldComponent, GetExplosionResistanceEvent>(OnVanguardShieldGetResistance);
        SubscribeLocalEvent<VanguardShieldComponent, RemovedShieldEvent>(OnVanguardShieldRemoved);
    }

    private void OnVanguardShieldInit(Entity<VanguardShieldComponent> xeno, ref MapInitEvent args)
    {
        RegenShield(xeno);
    }

    private void OnVanguardShieldHit(Entity<VanguardShieldComponent> xeno, ref DamageModifyAfterResistEvent args)
    {
        if (args.Damage.GetTotal() <= 0 || (!TryComp<XenoShieldComponent>(xeno, out var shield)) || shield.Shield != XenoShieldSystem.ShieldType.Vanguard)
            return;

        if (_net.IsServer)
            xeno.Comp.LastTimeHit = _timing.CurTime;

        if (!xeno.Comp.WasHit && args.Damage.GetTotal() > xeno.Comp.DecayThreshold)
        {
            xeno.Comp.WasHit = true;
            _popup.PopupEntity(Loc.GetString("rmc-xeno-shield-vanguard-hit"), xeno, xeno, PopupType.MediumCaution);

            args.Damage.ClampMax(0);
        }
    }

    private void OnVanguardShieldGetResistance(Entity<VanguardShieldComponent> xeno, ref GetExplosionResistanceEvent args)
    {
        if (!TryComp<XenoShieldComponent>(xeno, out var shield) || shield.Shield != XenoShieldSystem.ShieldType.Vanguard)
            return;

        if (shield.ShieldAmount <= 0)
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

            if (!shield.Active || !vanguardShield.WasHit || vanguardShield.NextDecay > time)
                continue;

            vanguardShield.NextDecay = time + vanguardShield.DecayEvery;

            shield.ShieldAmount = Math.Max(0, ((shield.ShieldAmount * vanguardShield.DecayMult) - vanguardShield.DecaySub).Double());

            if (shield.ShieldAmount <= 0)
                _shield.RemoveShield(uid, XenoShieldSystem.ShieldType.Vanguard);

            Dirty(uid, shield);
        }
    }

    //Returns whether something applies for the shield buff (for cleave)
    public bool ShieldBuff(EntityUid ent)
    {
        if (!TryComp<XenoShieldComponent>(ent, out var shield))
            return false;

        if (shield.Shield == XenoShieldSystem.ShieldType.Vanguard && shield.Active)
            return true;

        if (TryComp<VanguardShieldComponent>(ent, out var vanguard) && vanguard.NextDecay + vanguard.BuffExtraTime > _timing.CurTime)
            return true;

        return false;
    }

    public void RegenShield(EntityUid ent)
    {
        if (!TryComp<VanguardShieldComponent>(ent, out var vanguard) || !TryComp<XenoShieldComponent>(ent, out var shield))
            return;

        vanguard.LastRecharge = _timing.CurTime;
        vanguard.WasHit = false;

        if (!shield.Active && _net.IsServer)
            _popup.PopupEntity(Loc.GetString("rmc-xeno-shield-vanguard-regen"), ent, ent, PopupType.Medium);

        _shield.ApplyShield(ent, XenoShieldSystem.ShieldType.Vanguard, vanguard.RegenAmount, maxShield: vanguard.RegenAmount.Double());

    }
}
