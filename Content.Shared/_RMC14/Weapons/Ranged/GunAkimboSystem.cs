using Content.Shared._RMC14.CCVar;
using Content.Shared.GameTicking;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Weapons.Ranged;

public sealed class GunAkimboSystem : EntitySystem
{
    [Dependency] private readonly CMGunSystem _cmGun = default!;
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly SharedGunSystem _gun = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly INetConfigurationManager _netConfig = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private readonly Dictionary<EntityUid, (EntityUid Gun, GameTick Tick)> _pendingOffHandShots = new();

    public override void Initialize()
    {
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestartCleanup);

        SubscribeLocalEvent<GunDualWieldingComponent, GunShotEvent>(OnDualWieldingGunShot);
        SubscribeLocalEvent<GunDualWieldingComponent, OnEmptyGunShotEvent>(OnDualWieldingEmptyGunShot);
    }

    private void OnRoundRestartCleanup(RoundRestartCleanupEvent ev)
    {
        _pendingOffHandShots.Clear();
    }

    private void OnDualWieldingGunShot(Entity<GunDualWieldingComponent> gun, ref GunShotEvent args)
    {
        QueueOffHandShot(gun, args.User);

        if (TryComp(gun.Owner, out GunComponent? gunComp) && gunComp.BurstActivated)
            return;

        HandleAlternateHands(gun);
    }

    private void OnDualWieldingEmptyGunShot(Entity<GunDualWieldingComponent> gun, ref OnEmptyGunShotEvent args)
    {
        HandleAlternateHands(gun);
    }

    private void QueueOffHandShot(Entity<GunDualWieldingComponent> gun, EntityUid user)
    {
        if (!TryComp(user, out HandsComponent? hands) ||
            !TryComp(gun.Owner, out GunComponent? gunComp) ||
            _hands.GetActiveItem((user, hands)) != gun.Owner ||
            GetAkimboMode(user, gunComp) != GunAkimboMode.FireBoth ||
            !TryGetOtherDualWieldedGun((user, hands), gun, out var otherGun, out _))
        {
            return;
        }

        _pendingOffHandShots[user] = (otherGun.Owner, _timing.CurTick);
    }

    private void HandleAlternateHands(Entity<GunDualWieldingComponent> gun)
    {
        if (gun.Comp.WeaponGroup == GunDualWieldingGroup.None ||
            !TryComp(gun.Owner, out GunComponent? gunComp) ||
            !_cmGun.TryGetGunUser(gun.Owner, out var user) ||
            GetAkimboMode(user.Owner, gunComp) != GunAkimboMode.AlternateHands ||
            !_hands.IsHolding(user.AsNullable(), gun.Owner, out var currentHand) ||
            currentHand != user.Comp.ActiveHandId ||
            !TryGetOtherDualWieldedGun(user, gun, out _, out var otherHand))
        {
            return;
        }

        _gun.StopShooting((gun.Owner, gunComp));
        _hands.SetActiveHand(user.AsNullable(), otherHand);
    }

    public GunAkimboMode GetAkimboMode(EntityUid user)
    {
        if (_net.IsClient)
            return (GunAkimboMode) _config.GetCVar(RMCCVars.RMCAkimboMode);

        if (TryComp(user, out ActorComponent? actor))
            return (GunAkimboMode) _netConfig.GetClientCVar(actor.PlayerSession.Channel, RMCCVars.RMCAkimboMode);

        return GunAkimboMode.FireBoth;
    }

    public GunAkimboMode GetAkimboMode(EntityUid user, GunComponent gun)
    {
        var mode = GetAkimboMode(user);
        if (mode == GunAkimboMode.AlternateHands && gun.SelectedMode == SelectiveFire.FullAuto)
            return GunAkimboMode.FireBoth;

        return mode;
    }

    public bool IsHeldDualWieldGun(EntityUid user, EntityUid gun)
    {
        return TryComp(gun, out GunDualWieldingComponent? dualWielding) &&
               dualWielding.WeaponGroup != GunDualWieldingGroup.None &&
               TryComp(user, out HandsComponent? hands) &&
               _hands.IsHolding((user, hands), gun);
    }

    public bool HasPendingOffHandShot(EntityUid user, EntityUid gun)
    {
        return _pendingOffHandShots.TryGetValue(user, out var pending) &&
               pending.Gun == gun &&
               pending.Tick == _timing.CurTick;
    }

    public bool TryConsumeOffHandShot(EntityUid user, EntityUid gun)
    {
        if (!_pendingOffHandShots.Remove(user, out var pending) ||
            pending.Gun != gun ||
            pending.Tick != _timing.CurTick)
        {
            return false;
        }

        return IsHeldDualWieldGun(user, gun);
    }

    public bool TryGetOtherDualWieldedGun(
        Entity<HandsComponent> user,
        Entity<GunDualWieldingComponent> gun,
        out Entity<GunDualWieldingComponent> otherGun,
        out string otherHand)
    {
        otherGun = default;
        otherHand = string.Empty;

        if (gun.Comp.WeaponGroup == GunDualWieldingGroup.None)
            return false;

        foreach (var hand in user.Comp.Hands.Keys)
        {
            if (_hands.GetHeldItem(user.AsNullable(), hand) is not { } held ||
                held == gun.Owner ||
                !TryComp(held, out GunDualWieldingComponent? dualWieldingComp) ||
                dualWieldingComp.WeaponGroup == GunDualWieldingGroup.None ||
                HasComp<GunRequiresWieldComponent>(held))
            {
                continue;
            }

            otherGun = (held, dualWieldingComp);
            otherHand = hand;
            return true;
        }

        return false;
    }
}
