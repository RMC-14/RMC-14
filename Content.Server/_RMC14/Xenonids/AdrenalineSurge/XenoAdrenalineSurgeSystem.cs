using Content.Shared._RMC14.Damage;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.Projectiles;
using Robust.Shared.Timing;

namespace Content.Server._RMC14.Xenonids.AdrenalineSurge;

public sealed class XenoAdrenalineSurgeSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _speedModifier = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<XenoAdrenalineSurgeComponent, DamageModifyAfterResistEvent>(OnDamageTaken);
        SubscribeLocalEvent<XenoAdrenalineSurgeComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshSpeed);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<XenoAdrenalineSurgeComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            var now = _timing.CurTime;

            if (comp.IsSurging && comp.SurgeEndTime.HasValue && now >= comp.SurgeEndTime.Value)
            {
                EndSurge(uid, comp);
            }

            if (!comp.IsUsable && !comp.IsSurging)
            {
                var cooldownExpiry = (comp.SurgeEndTime ?? TimeSpan.Zero) + comp.Cooldown;
                if (now >= cooldownExpiry)
                {
                    comp.IsUsable = true;

                    if (!comp.ReadyMessageSent)
                    {
                        comp.ReadyMessageSent = true;
                        _popup.PopupEntity(Loc.GetString("rmc-xeno-adrenaline-surge-ready"), uid, uid);
                    }
                }
            }
        }
    }

    private void OnDamageTaken(EntityUid uid, XenoAdrenalineSurgeComponent comp, ref DamageModifyAfterResistEvent args)
    {
        if (!comp.IsUsable)
            return;

        if (args.Tool == null || !HasComp<ProjectileComponent>(args.Tool))
            return;

        StartSurge(uid, comp);
    }

    private void StartSurge(EntityUid uid, XenoAdrenalineSurgeComponent comp)
    {
        comp.IsSurging = true;
        comp.IsUsable = false;
        comp.ReadyMessageSent = false;
        comp.SurgeEndTime = _timing.CurTime + comp.SurgeDuration;
        _popup.PopupEntity(Loc.GetString("rmc-xeno-adrenaline-surge-start"), uid, uid);
        _speedModifier.RefreshMovementSpeedModifiers(uid);
    }

    private void EndSurge(EntityUid uid, XenoAdrenalineSurgeComponent comp)
    {
        comp.IsSurging = false;
        _speedModifier.RefreshMovementSpeedModifiers(uid);
    }

    private void OnRefreshSpeed(EntityUid uid, XenoAdrenalineSurgeComponent comp, RefreshMovementSpeedModifiersEvent args)
    {
        if (!comp.IsSurging)
            return;

        args.ModifySpeed(1.35f, 1.35f);
    }
}
