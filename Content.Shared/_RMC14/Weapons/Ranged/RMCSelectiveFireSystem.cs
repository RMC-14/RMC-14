using Content.Shared._RMC14.Attachable.Systems;
using Content.Shared._RMC14.Input;
using Content.Shared.Examine;
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

    private const string scatterExamineColour = "yellow";

    private const SelectiveFire allFireModes = SelectiveFire.SemiAuto | SelectiveFire.Burst | SelectiveFire.FullAuto;

    public override void Initialize()
    {
        SubscribeAllEvent<RequestStopShootEvent>(OnStopShootRequest);

        SubscribeLocalEvent<RMCSelectiveFireComponent, ExaminedEvent>(OnExamine);
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

    private void OnExamine(Entity<RMCSelectiveFireComponent> gun, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange || !TryComp(gun.Owner, out GunComponent? gunComponent))
            return;

        using (args.PushGroup(nameof(RMCSelectiveFireComponent)))
        {
            args.PushMarkup(Loc.GetString("rmc-examine-text-scatter-max", ("colour", scatterExamineColour), ("scatter", gunComponent.MaxAngleModified.Degrees)));
            args.PushMarkup(Loc.GetString("rmc-examine-text-scatter-min", ("colour", scatterExamineColour), ("scatter", gunComponent.MinAngleModified.Degrees)));

            if (ContainsMods(gun, gunComponent.SelectedMode))
            {
                var mods = gun.Comp.Modifiers[gunComponent.SelectedMode];
                if (mods.ShotsToMaxScatter != null)
                    args.PushMarkup(Loc.GetString("rmc-examine-text-shots-to-max-scatter", ("colour", scatterExamineColour), ("shots", mods.ShotsToMaxScatter)));
            }
        }
    }

#region Refresh calls
    private void OnSelectiveFireMapInit(Entity<RMCSelectiveFireComponent> gun, ref MapInitEvent args)
    {
        gun.Comp.BurstScatterMultModified = gun.Comp.BurstScatterMult;
        RefreshFireModes((gun.Owner, gun.Comp), true);
    }

    private void OnSelectiveFireModeChanged(Entity<RMCSelectiveFireComponent> gun, ref RMCFireModeChangedEvent args)
    {
        RefreshFireModeGunValues(gun);
    }

    private void SelectiveFireRefreshWield<T>(Entity<RMCSelectiveFireComponent> gun, ref T args) where T : notnull
    {
        RefreshWieldableFireModeValues(gun);
    }
#endregion

#region Refresh methods
    public void RefreshFireModeGunValues(Entity<RMCSelectiveFireComponent> gun)
    {
        if (!TryComp(gun.Owner, out GunComponent? gunComponent))
            return;

        gunComponent.AngleIncrease = gun.Comp.ScatterIncrease;
        gunComponent.AngleDecay = gun.Comp.ScatterDecay;

        var ev = new GunGetFireRateEvent(gunComponent.SelectedMode == SelectiveFire.Burst ? gun.Comp.BaseFireRate * 2 : gun.Comp.BaseFireRate);
        RaiseLocalEvent(gun, ref ev);
        gunComponent.FireRate = ev.FireRate;

        if (ContainsMods(gun, gunComponent.SelectedMode))
        {
            var mods = gun.Comp.Modifiers[gunComponent.SelectedMode];
            ev = new GunGetFireRateEvent(1f / (1f / gunComponent.FireRate + mods.FireDelay));
            RaiseLocalEvent(gun, ref ev);
            gunComponent.FireRate = ev.FireRate;
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

    public void RefreshFireModes(Entity<RMCSelectiveFireComponent?> gun, bool forceValueRefresh = false)
    {
        if (gun.Comp == null && !TryComp(gun.Owner, out gun.Comp) || !TryComp(gun.Owner, out GunComponent? gunComponent))
            return;

        var initialMode = gunComponent.SelectedMode;

        var ev = new GetFireModesEvent(gun.Comp.BaseFireModes);
        RaiseLocalEvent(gun.Owner, ref ev);

        SetFireModes((gun.Owner, gunComponent), ev.Modes, !(forceValueRefresh || initialMode != gunComponent.SelectedMode));

        if (TryComp(gun, out GunComponent? gunComp) &&
            (gunComp.AvailableModes & ev.Set) != SelectiveFire.Invalid)
        {
            _gunSystem.SelectFire(gun, gunComponent, ev.Set);
        }

        if (forceValueRefresh || initialMode != gunComponent.SelectedMode)
            RefreshFireModeGunValues((gun.Owner, gun.Comp));
    }

    public void RefreshModifiableFireModeValues(Entity<RMCSelectiveFireComponent?> gun)
    {
        if (gun.Comp == null && !TryComp(gun.Owner, out gun.Comp))
            return;

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
#endregion

#region Firemode changes
    public void AddFireMode(Entity<GunComponent?> gun, SelectiveFire newMode)
    {
        if (gun.Comp == null && !TryComp(gun.Owner, out gun.Comp))
            return;

        gun.Comp.AvailableModes |= newMode;
        Dirty(gun);
    }

    public void SetFireModes(Entity<GunComponent?> gun, SelectiveFire modes, bool dirty = true)
    {
        if (gun.Comp == null && !TryComp(gun.Owner, out gun.Comp) || (modes & allFireModes) == SelectiveFire.Invalid)
            return;

        gun.Comp.AvailableModes = allFireModes;

        while ((gun.Comp.SelectedMode & modes) != gun.Comp.SelectedMode)
            _gunSystem.CycleFire(gun.Owner, gun.Comp);

        gun.Comp.AvailableModes = modes;

        if (!dirty)
            return;

        Dirty(gun);
    }
#endregion
}
