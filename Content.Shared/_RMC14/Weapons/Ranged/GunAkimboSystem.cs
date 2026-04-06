using Content.Shared._RMC14.CCVar;
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

    public override void Initialize()
    {
        SubscribeLocalEvent<GunDualWieldingComponent, GunShotEvent>(OnDualWieldingGunShot);
        SubscribeLocalEvent<GunDualWieldingComponent, OnEmptyGunShotEvent>(OnDualWieldingEmptyGunShot);
    }

    private void OnDualWieldingGunShot(Entity<GunDualWieldingComponent> gun, ref GunShotEvent args)
    {
        HandleAlternateHands(gun);
    }

    private void OnDualWieldingEmptyGunShot(Entity<GunDualWieldingComponent> gun, ref OnEmptyGunShotEvent args)
    {
        HandleAlternateHands(gun);
    }

    private void HandleAlternateHands(Entity<GunDualWieldingComponent> gun)
    {
        if (_net.IsClient && !_timing.IsFirstTimePredicted)
            return;

        if (gun.Comp.WeaponGroup == GunDualWieldingGroup.None ||
            !_cmGun.TryGetGunUser(gun.Owner, out var user) ||
            GetAkimboMode(user.Owner) != GunAkimboMode.AlternateHands ||
            !_hands.IsHolding(user.AsNullable(), gun.Owner, out var currentHand) ||
            currentHand != user.Comp.ActiveHandId ||
            !TryGetOtherDualWieldedGun(user, gun, out var otherGun, out var otherHand))
        {
            return;
        }

        if (TryComp(gun.Owner, out GunComponent? gunComp))
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

    public bool TryGetOtherDualWieldedGun(
        Entity<HandsComponent> user,
        Entity<GunDualWieldingComponent> gun,
        out Entity<GunDualWieldingComponent> otherGun,
        out string otherHand)
    {
        otherGun = default;
        otherHand = string.Empty;

        foreach (var hand in user.Comp.Hands.Keys)
        {
            if (_hands.GetHeldItem(user.AsNullable(), hand) is not { } held ||
                held == gun.Owner ||
                !TryComp(held, out GunDualWieldingComponent? dualWieldingComp) ||
                dualWieldingComp.WeaponGroup == GunDualWieldingGroup.None)
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
