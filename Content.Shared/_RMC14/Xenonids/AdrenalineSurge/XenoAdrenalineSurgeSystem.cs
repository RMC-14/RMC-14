using Content.Shared._RMC14.Damage;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.Projectiles;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Xenonids.AdrenalineSurge;

public sealed class XenoAdrenalineSurgeSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _speedModifier = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<XenoAdrenalineSurgeComponent, DamageModifyAfterResistEvent>(OnDamageTaken);
        SubscribeLocalEvent<XenoAdrenalineSurgeComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshSpeed);
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<XenoAdrenalineSurgeComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            var now = _timing.CurTime;

            if (comp.IsSurging && comp.SurgeEndTime.HasValue && now >= comp.SurgeEndTime.Value)
            {
                EndSurge((uid, comp));
            }

            if (!comp.IsUsable && !comp.IsSurging)
            {
                var cooldownExpiry = (comp.SurgeEndTime ?? TimeSpan.Zero) + comp.Cooldown;
                if (now >= cooldownExpiry)
                {
                    comp.IsUsable = true;

                    _popup.PopupClient(Loc.GetString("rmc-xeno-adrenaline-surge-ready"), uid, uid);
                }
            }
        }
    }

    private void OnDamageTaken(Entity<XenoAdrenalineSurgeComponent> xeno, ref DamageModifyAfterResistEvent args)
    {
        if (!xeno.Comp.IsUsable)
            return;

        if (args.Tool == null || !HasComp<ProjectileComponent>(args.Tool))
            return;

        StartSurge(xeno);
    }

    private void StartSurge(Entity<XenoAdrenalineSurgeComponent> xeno)
    {
        xeno.Comp.IsSurging = true;
        xeno.Comp.IsUsable = false;
        xeno.Comp.SurgeEndTime = _timing.CurTime + xeno.Comp.SurgeDuration;
        Dirty(xeno);
        _popup.PopupEntity(Loc.GetString("rmc-xeno-adrenaline-surge-start"), xeno, xeno);
        _speedModifier.RefreshMovementSpeedModifiers(xeno);
    }

    private void EndSurge(Entity<XenoAdrenalineSurgeComponent> xeno)
    {
        xeno.Comp.IsSurging = false;
        Dirty(xeno);
        _speedModifier.RefreshMovementSpeedModifiers(xeno);
    }

    private void OnRefreshSpeed(Entity<XenoAdrenalineSurgeComponent> xeno, ref RefreshMovementSpeedModifiersEvent args)
    {
        if (!xeno.Comp.IsSurging)
            return;

        args.ModifySpeed(xeno.Comp.SpeedModifierAmount, xeno.Comp.SpeedModifierAmount);
    }
}
