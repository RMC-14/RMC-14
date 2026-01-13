using Content.Shared._RMC14.Armor;
using Content.Shared._RMC14.Marines;
using Content.Shared._RMC14.Xenonids;
using Content.Shared.Interaction.Events;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Projectiles;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Weapons.Ranged.Stacks;

public sealed class GunStacksSystem : EntitySystem
{
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly CMArmorSystem _rmcArmor = default!;
    [Dependency] private readonly CMGunSystem _rmcGun = default!;
    [Dependency] private readonly RMCSelectiveFireSystem _rmcSelectiveFire = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private EntityQuery<GunStacksComponent> _gunStacksQuery;
    private EntityQuery<RMCSelectiveFireComponent> _selectiveFireQuery;
    private EntityQuery<XenoComponent> _xenoQuery;
    private EntityQuery<MarineComponent> _marineQuery;
    private EntityQuery<RMCAdjustableArmorValueComponent> _adjustableArmor;

    public override void Initialize()
    {
        _gunStacksQuery = GetEntityQuery<GunStacksComponent>();
        _selectiveFireQuery = GetEntityQuery<RMCSelectiveFireComponent>();
        _xenoQuery = GetEntityQuery<XenoComponent>();
        _marineQuery = GetEntityQuery<MarineComponent>();
        _adjustableArmor = GetEntityQuery<RMCAdjustableArmorValueComponent>();
        SubscribeLocalEvent<GunStacksComponent, AmmoShotEvent>(OnStacksAmmoShot);

        SubscribeLocalEvent<GunStacksActiveComponent, GetGunDamageModifierEvent>(OnStacksActiveGetGunDamageModifier);
        SubscribeLocalEvent<GunStacksActiveComponent, GunGetFireRateEvent>(OnStacksActiveGetGunFireRate);
        SubscribeLocalEvent<GunStacksActiveComponent, DroppedEvent>(OnStacksActiveDropped);

        SubscribeLocalEvent<GunStacksProjectileComponent, ProjectileHitEvent>(OnStacksProjectileHit);
    }

    private void OnStacksAmmoShot(Entity<GunStacksComponent> ent, ref AmmoShotEvent args)
    {
        foreach (var bullet in args.FiredProjectiles)
        {
            var stacks = EnsureComp<GunStacksProjectileComponent>(bullet);
            stacks.Gun = ent;
            Dirty(bullet, stacks);

            var piercing = EnsureComp<CMArmorPiercingComponent>(bullet);
            var shotsHit = 0;

            if (TryComp<GunStacksActiveComponent>(ent, out var active))
                shotsHit = active.Hits;

            var ap = Math.Min(ent.Comp.MaxAP, ent.Comp.IncreaseAP * shotsHit);
            _rmcArmor.SetArmorPiercing((bullet, piercing), ap);
        }
    }

    private void OnStacksActiveGetGunDamageModifier(Entity<GunStacksActiveComponent> ent, ref GetGunDamageModifierEvent args)
    {
        if (!TryComp<GunStacksComponent>(ent, out var gunStacks))
            return;

        if (ent.Comp.Hits > 0)
            args.Multiplier += gunStacks.DamageIncrease;
    }

    private void OnStacksActiveGetGunFireRate(Entity<GunStacksActiveComponent> ent, ref GunGetFireRateEvent args)
    {
        if (!TryComp<GunStacksComponent>(ent, out var gunStacks))
            return;

        if (ent.Comp.Hits > 0)
            args.FireRate = gunStacks.SetFireRate;
    }

    private void OnStacksActiveDropped(Entity<GunStacksActiveComponent> ent, ref DroppedEvent args)
    {
        Reset(ent);
    }

    private void OnStacksProjectileHit(Entity<GunStacksProjectileComponent> ent, ref ProjectileHitEvent args)
    {
        if (ent.Comp.Gun == null)
            return;

        if (!_gunStacksQuery.HasComp(ent.Comp.Gun))
            return;

        if (TryComp(ent, out ProjectileComponent? projectile) &&
            projectile.ProjectileSpent)
        {
            return;
        }

        var target = args.Target;
        if ((_xenoQuery.HasComp(target) || _marineQuery.HasComp(target) || _adjustableArmor.HasComp(target)) && !_mobState.IsDead(target))
        {
            if (!TryComp<GunStacksActiveComponent>(ent.Comp.Gun, out var gun))
                gun = EnsureComp<GunStacksActiveComponent>(ent.Comp.Gun.Value);

            // Reset the counter if the new target is a xeno while the last hit target was a target dummy.
            if (gun.LastHitEntity != null &&
                HasComp<RMCAdjustableArmorValueComponent>(gun.LastHitEntity.Value) &&
                HasComp<XenoComponent>(target))
            {
                Reset((ent.Comp.Gun.Value, gun));
                return;
            }

            gun.Hits++;
            gun.ExpireAt = _timing.CurTime + gun.StacksExpire;
            gun.LastHitEntity = target;
            if (args.Shooter is { } shooter &&
                _net.IsServer)
            {
                var msg = gun.Hits == 1 ? Loc.GetString("rmc-gun-stacks-hit-single") : Loc.GetString("rmc-gun-stacks-hit-multiple", ("hits", gun.Hits));
                _popup.PopupEntity(msg, shooter, shooter);
            }
        }

        RefreshGunStats(ent.Comp.Gun.Value);

        Dirty(ent);
    }

    private void Reset(Entity<GunStacksActiveComponent> gun)
    {
        RemComp<GunStacksActiveComponent>(gun.Owner);
        if (_net.IsServer)
            _popup.PopupEntity(Loc.GetString("rmc-gun-stacks-reset", ("weapon", gun.Owner)), gun, PopupType.SmallCaution);

        RefreshGunStats(gun.Owner);
    }

    private void RefreshGunStats(EntityUid gun)
    {
        _rmcGun.RefreshGunDamageMultiplier(gun);

        if (_selectiveFireQuery.TryComp(gun, out var selective))
        {
            _rmcSelectiveFire.RefreshFireModeGunValues((gun, selective));
            Dirty(gun, selective);
        }
    }

    public override void Update(float frameTime)
    {
        var time = _timing.CurTime;

        var gunStackQuery = EntityQueryEnumerator<GunStacksActiveComponent>();
        while (gunStackQuery.MoveNext(out var uid, out var active))
        {
            if (active.ExpireAt > time)
                continue;

            Reset((uid, active));
        }
    }
}
