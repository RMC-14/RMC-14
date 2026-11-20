using Content.Shared._RMC14.Dropship.Weapon;
using Content.Shared._RMC14.Item.Deploy;
using Content.Shared._RMC14.Marines;
using Content.Shared._RMC14.Marines.Orders;
using Content.Shared._RMC14.Marines.Squads;
using Content.Shared._RMC14.Weapons.Ranged.IFF;
using Content.Shared.Interaction;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Robust.Shared.Network;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Sentry.Flag;

public sealed class PlantedFlagSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedDropshipWeaponSystem _dropshipWeapon = default!;
    [Dependency] private readonly EntityLookupSystem _entLookup = default!;
    [Dependency] private readonly GunIFFSystem _gunIFF = default!;
    [Dependency] private readonly FixtureSystem _fixture = default!;
    [Dependency] private readonly SharedMarineOrdersSystem _marineOrders = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedPointLightSystem _pointLight = default!;
    [Dependency] private readonly SentrySystem _sentry = default!;
    [Dependency] private readonly SquadSystem _squad = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;

    private const string DropshipFaction = "FactionMarine";

    public override void Initialize()
    {
        SubscribeLocalEvent<PlantedFlagComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<PlantedFlagComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<PlantedFlagComponent, ActivateInWorldEvent>(OnFlagActivateInWorld);
        SubscribeLocalEvent<PlantedFlagComponent, EquipmentDeployedEvent>(OnDeployed);
        SubscribeLocalEvent<PlantedFlagComponent, EquipmentUnDeployedEvent>(OnUndeployed);
        SubscribeLocalEvent<PlantedFlagComponent, SentryUpgradedEvent>(OnUpgraded);


        Subs.BuiEvents<PlantedFlagComponent>(SentryUiKey.Key,
            subs =>
            {
                subs.Event<SentryUpgradeBuiMsg>(OnFlagUpgradeBuiMsg);
            });
    }

    private void OnMapInit(Entity<PlantedFlagComponent> ent, ref MapInitEvent args)
    {
        if (_net.IsServer)
        {
            ent.Comp.Id = _dropshipWeapon.ComputeNextId();
            Dirty(ent);
        }

        UpdateState(ent);
    }

    private void OnInteractUsing(Entity<PlantedFlagComponent> ent, ref InteractUsingEvent args)
    {
        if (!TryComp(args.Used, out SentryUpgradeItemComponent? upgrade))
            return;

        OpenUpgradeMenu(ent, (args.Used, upgrade), args.User);
    }

    private void OnFlagActivateInWorld(Entity<PlantedFlagComponent> flag, ref ActivateInWorldEvent args)
    {
        ref var mode = ref flag.Comp.Mode;
        if (mode == FlagMode.Item)
            return;

        args.Handled = true;

        var user = args.User;

        switch (mode)
        {
            case FlagMode.Off:
            {
                mode = FlagMode.On;
                if (flag.Comp.Id is { } id && _gunIFF.IsInFaction(flag, DropshipFaction))
                {
                    var abbreviation = _dropshipWeapon.GetUserAbbreviation(user, id);
                    _dropshipWeapon.MakeDropshipTarget(flag, abbreviation);
                }
                break;
            }
            default:
            {
                mode = FlagMode.Off;
                _dropshipWeapon.TryRemoveTarget(flag);

                break;
            }
        }

        Dirty(flag);
        UpdateState(flag);
    }

    private void OnDeployed(Entity<PlantedFlagComponent> ent, ref EquipmentDeployedEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        ent.Comp.Mode = FlagMode.Off;
        Dirty(ent);

        UpdateState(ent);
    }

    private void OnUndeployed(Entity<PlantedFlagComponent> ent, ref EquipmentUnDeployedEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        ent.Comp.Mode = FlagMode.Item;
        Dirty(ent);

        UpdateState(ent);
    }

    private void OnFlagUpgradeBuiMsg(Entity<PlantedFlagComponent> flag, ref SentryUpgradeBuiMsg args)
    {
        _sentry.TryUpgradeSentry(flag, args.Actor, args.Upgrade, flag.Comp.Upgrades);
    }

    private void OnUpgraded(Entity<PlantedFlagComponent> flag, ref SentryUpgradedEvent args)
    {
        if (!TryComp(args.OldSentry, out PlantedFlagComponent? oldFlagComponent))
            return;

        flag.Comp.Id = oldFlagComponent.Id;
        Dirty(flag);
    }

    private void UpdateState(Entity<PlantedFlagComponent> flag)
    {
        var fixture = flag.Comp.DeployFixture is { } fixtureId && TryComp(flag, out FixturesComponent? fixtures)
            ? _fixture.GetFixtureOrNull(flag, fixtureId, fixtures)
            : null;

        switch (flag.Comp.Mode)
        {
            case FlagMode.Item:
                if (fixture != null)
                    _physics.SetHard(flag, fixture, false);

                _appearance.SetData(flag, FlagLayers.Layer, FlagMode.Item);
                _pointLight.SetEnabled(flag, false);
                break;
            case FlagMode.Off:
                if (fixture != null)
                    _physics.SetHard(flag, fixture, true);

                _appearance.SetData(flag, FlagLayers.Layer, FlagMode.Off);
                _pointLight.SetEnabled(flag, false);
                break;
            case FlagMode.On:
                if (fixture != null)
                    _physics.SetHard(flag, fixture, true);

                _appearance.SetData(flag, FlagLayers.Layer, FlagMode.On);
                _pointLight.SetEnabled(flag, true);
                break;
        }
    }

    private void OpenUpgradeMenu(Entity<PlantedFlagComponent> flag, Entity<SentryUpgradeItemComponent> upgrade, EntityUid user)
    {
        if (flag.Comp.Mode != FlagMode.Item)
        {
            var msg = Loc.GetString("rmc-sentry-upgrade-not-item", ("sentry", flag));
            _popup.PopupClient(msg, user, user, PopupType.SmallCaution);
            return;
        }

        if (!_sentry.CanUpgradePopup(flag, ref upgrade, user, flag.Comp.Upgrades, null))
            return;

        _ui.OpenUi(flag.Owner, SentryUiKey.Key, user);
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<PlantedFlagComponent>();
        while (query.MoveNext(out var uid, out var flag))
        {
            if (flag.Mode != FlagMode.On)
                continue;

            if (_timing.CurTime < flag.NextOrder)
                continue;

            foreach (var entity in _entLookup.GetEntitiesInRange<MarineComponent>(_transform.GetMoverCoordinates(uid), flag.Range))
            {
                if (_mobState.IsDead(entity))
                    continue;

                var factions = new HashSet<EntProtoId<IFFFactionComponent>>();
                if (!_gunIFF.TryGetFactions(uid, factions))
                    continue;

                var isInFaction = false;
                foreach (var faction in factions)
                {
                    if (!_gunIFF.IsInFaction(entity, faction))
                        continue;

                    isInFaction = true;
                    break;
                }

                if (!isInFaction)
                    continue;

                if (flag.ApplyFocus)
                    _marineOrders.AddOrder<FocusOrderComponent>(entity, flag.Strength, flag.Duration);
                if (flag.ApplyHold)
                    _marineOrders.AddOrder<HoldOrderComponent>(entity, flag.Strength, flag.Duration);
                if (flag.ApplyMove)
                    _marineOrders.AddOrder<MoveOrderComponent>(entity, flag.Strength, flag.Duration);

                flag.NextOrder = _timing.CurTime + flag.Cooldown;
                Dirty(uid, flag);
            }
        }
    }
}
