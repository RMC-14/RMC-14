using System.Numerics;
using Content.Shared._RMC14.Attachable.Systems;
using Content.Shared._RMC14.Input;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Content.Shared.Wieldable;
using Content.Shared.Wieldable.Components;
using Robust.Shared.Input.Binding;

namespace Content.Shared._RMC14.Weapons.Ranged;

public sealed class RMCSelectiveFireSystem : EntitySystem
{
    [Dependency] private readonly SharedGunSystem _gunSystem = default!;

    public override void Initialize()
    {
        SubscribeAllEvent<RequestStopShootEvent>(OnStopShootRequest);

        SubscribeLocalEvent<RMCSelectiveFireComponent, ItemWieldedEvent>(SelectiveFireRefreshWield,
            after: new[] { typeof(AttachableHolderSystem) });
        SubscribeLocalEvent<RMCSelectiveFireComponent, ItemUnwieldedEvent>(SelectiveFireRefreshWield,
            after: new[] { typeof(AttachableHolderSystem) });
        SubscribeLocalEvent<RMCSelectiveFireComponent, MapInitEvent>(OnSelectiveFireMapInit);
        SubscribeLocalEvent<RMCSelectiveFireComponent, RMCFireModeChangedEvent>(OnSelectiveFireModeChanged);

        CommandBinds.Builder
            .Bind(CMKeyFunctions.RMCCycleFireMode,
                InputCmdHandler.FromDelegate(session =>
                    {
                        if (session?.AttachedEntity is { } userUid && _gunSystem.TryGetGun(userUid, out var gunUid, out var gunComponent))
                        {
                            _gunSystem.CycleFire(gunUid, gunComponent, userUid);
                        }
                    },
                    handle: false))
            .Register<RMCSelectiveFireSystem>();
    }

    private void OnStopShootRequest(RequestStopShootEvent ev, EntitySessionEventArgs args)
    {
        var gunUid = GetEntity(ev.Gun);

        if (args.SenderSession.AttachedEntity == null ||
            !TryComp(gunUid, out GunComponent? gunComponent) ||
            !_gunSystem.TryGetGun(args.SenderSession.AttachedEntity.Value, out _, out var userGun))
        {
            return;
        }

        if (userGun != gunComponent)
            return;

        gunComponent.CurrentAngle = gunComponent.MinAngleModified;
        Dirty(gunUid, gunComponent);
    }

    private void OnSelectiveFireMapInit(Entity<RMCSelectiveFireComponent> gun, ref MapInitEvent args)
    {
        gun.Comp.BurstScatterMultModified = gun.Comp.BurstScatterMult;
        RefreshFireModeGunValues(gun);
    }

    private void OnSelectiveFireModeChanged(Entity<RMCSelectiveFireComponent> gun, ref RMCFireModeChangedEvent args)
    {
        RefreshFireModeGunValues(gun);
    }

    private void SelectiveFireRefreshWield<T>(Entity<RMCSelectiveFireComponent> gun, ref T args) where T : notnull
    {
        RefreshWieldableFireModeValues(gun);
    }

    public void RefreshFireModeGunValues(Entity<RMCSelectiveFireComponent> gun)
    {
        if (!TryComp(gun.Owner, out GunComponent? gunComponent))
            return;

        gunComponent.AngleIncrease = gun.Comp.BaseAngleIncrease;
        gunComponent.AngleDecay = gun.Comp.BaseAngleDecay;
        gunComponent.FireRate = gunComponent.SelectedMode == SelectiveFire.Burst ? gun.Comp.BaseFireRate * 2 : gun.Comp.BaseFireRate;

        if (ContainsMods(gun, gunComponent.SelectedMode))
        {
            var mods = gun.Comp.Modifiers[gunComponent.SelectedMode];
            gunComponent.FireRate = 1f / (1f / gunComponent.FireRate + mods.FireDelay);
        }

        RefreshWieldableFireModeValues(gun);
    }

    public bool ContainsMods(Entity<RMCSelectiveFireComponent> gun, SelectiveFire mode)
    {
        return gun.Comp.Modifiers.ContainsKey(mode);
    }

    public void RefreshWieldableFireModeValues(Entity<RMCSelectiveFireComponent> gun)
    {
        if (!TryComp(gun.Owner, out GunComponent? gunComponent))
            return;

        bool wielded = TryComp(gun.Owner, out WieldableComponent? wieldableComponent) && wieldableComponent.Wielded;

        gunComponent.CameraRecoilScalar = wielded ? gun.Comp.RecoilWielded : gun.Comp.RecoilUnwielded;
        gunComponent.MinAngle = wielded ? gun.Comp.ScatterWielded : gun.Comp.ScatterUnwielded;
        gunComponent.MaxAngle = gunComponent.MinAngle;

        RefreshBurstScatter((gun.Owner, gun.Comp));

        _gunSystem.RefreshModifiers(gun.Owner);
        gunComponent.CurrentAngle = gunComponent.MinAngleModified;
    }

    public void RefreshModifiableFireModeValues(Entity<RMCSelectiveFireComponent?> gun)
    {
        if (gun.Comp == null)
        {
            if (!TryComp(gun.Owner, out RMCSelectiveFireComponent? selectiveFireComponent))
                return;

            gun.Comp = selectiveFireComponent;
        }

        var ev = new GetFireModeValuesEvent(gun.Comp.BurstScatterMult);
        RaiseLocalEvent(gun.Owner, ref ev);

        gun.Comp.BurstScatterMultModified = ev.BurstScatterMult;

        RefreshWieldableFireModeValues((gun.Owner, gun.Comp));
    }

    private void RefreshBurstScatter(Entity<RMCSelectiveFireComponent> gun)
    {
        if (!TryComp(gun.Owner, out GunComponent? gunComponent))
            return;

        bool wielded = TryComp(gun.Owner, out WieldableComponent? wieldableComponent) && wieldableComponent.Wielded;

        if (ContainsMods(gun, gunComponent.SelectedMode))
        {
            var mods = gun.Comp.Modifiers[gunComponent.SelectedMode];
            var mult = mods.UseBurstScatterMult ? gun.Comp.BurstScatterMultModified : 1.0;
            gunComponent.MaxAngle = wielded
                ? Angle.FromDegrees(Math.Max(gunComponent.MinAngle.Degrees + mods.MaxScatterModifier * mult, gunComponent.MinAngle.Degrees))
                : Angle.FromDegrees(Math.Max(gunComponent.MinAngle.Degrees + mods.MaxScatterModifier * mult * mods.UnwieldedScatterMultiplier, gunComponent.MinAngle.Degrees));

            if (mods.ShotsToMaxScatter != null)
                gunComponent.AngleIncrease = new Angle(((double)(gunComponent.MaxAngle - gunComponent.MinAngle)) / mods.ShotsToMaxScatter.Value);
        }
    }
}
