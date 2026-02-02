using System.Diagnostics.CodeAnalysis;
using Content.Shared._RMC14.Atmos;
using Content.Shared._RMC14.Chemistry.Reagent;
using Content.Shared._RMC14.Fluids;
using Content.Shared._RMC14.Line;
using Content.Shared._RMC14.Map;
using Content.Shared._RMC14.Weapons.Common;
using Content.Shared._RMC14.Xenonids;
using Content.Shared.Actions;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Popups;
using Content.Shared.Temperature;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.Weapons.Ranged.Flamer;

public abstract class SharedRMCFlamerSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _action = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly ExamineSystemShared _examine = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly LineSystem _line = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedRMCFlammableSystem _rmcFlammable = default!;
    [Dependency] private readonly SharedRMCSpraySystem _rmcSpray = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;
    [Dependency] private readonly SolutionTransferSystem _solutionTransfer = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly RMCMapSystem _rmcMap = default!;
    [Dependency] private readonly RMCReagentSystem _reagent = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCFlamerAmmoProviderComponent, MapInitEvent>(OnMapInit, after: [typeof(SharedSolutionContainerSystem)]);
        SubscribeLocalEvent<RMCFlamerAmmoProviderComponent, TakeAmmoEvent>(OnTakeAmmo);
        SubscribeLocalEvent<RMCFlamerAmmoProviderComponent, GetAmmoCountEvent>(OnGetAmmoCount);
        SubscribeLocalEvent<RMCFlamerAmmoProviderComponent, EntInsertedIntoContainerMessage>(OnInsertedIntoContainer);
        SubscribeLocalEvent<RMCFlamerAmmoProviderComponent, EntRemovedFromContainerMessage>(OnRemovedFromContainer);
        SubscribeLocalEvent<RMCFlamerAmmoProviderComponent, AttemptShootEvent>(OnAttemptShoot);

        SubscribeLocalEvent<RMCFlamerTankComponent, BeforeRangedInteractEvent>(OnFlamerTankBeforeRangedInteract);
        SubscribeLocalEvent<RMCFlamerTankComponent, GetVerbsEvent<ExamineVerb>>(OnFlamerTankVerbExamine);

        SubscribeLocalEvent<RMCSprayAmmoProviderComponent, TakeAmmoEvent>(OnSprayTakeAmmo);
        SubscribeLocalEvent<RMCSprayAmmoProviderComponent, GetAmmoCountEvent>(OnSprayGetAmmoCount);

        SubscribeLocalEvent<RMCIgniterComponent, MapInitEvent>(OnIgniterMapInit, after: [typeof(SharedSolutionContainerSystem)]);
        SubscribeLocalEvent<RMCIgniterComponent, UniqueActionEvent>(OnIgniterUniqueAction);
        SubscribeLocalEvent<RMCIgniterComponent, IsHotEvent>(OnIgniterToggle);
        SubscribeLocalEvent<RMCIgniterComponent, AttemptShootEvent>(OnIgniterAttemptShoot);
        SubscribeLocalEvent<RMCIgniterComponent, ExaminedEvent>(OnIgniterUniqueActionExamine, before: [typeof(SharedGunSystem)]);

        SubscribeLocalEvent<RMCBroilerComponent, GetItemActionsEvent>(OnBroilerGetItemActions);
        SubscribeLocalEvent<RMCBroilerComponent, RMCBroilerActionEvent>(OnBroilerAction);

        SubscribeLocalEvent<RMCCanUseBroilerComponent, UniqueActionEvent>(OnBroilerUniqueAction);
        SubscribeLocalEvent<RMCCanUseBroilerComponent, ExaminedEvent>(OnBroilerUniqueActionExamine, before: [typeof(SharedGunSystem)]);
    }

    private void OnMapInit(Entity<RMCFlamerAmmoProviderComponent> ent, ref MapInitEvent args)
    {
        UpdateAppearance(ent);
    }

    private void OnTakeAmmo(Entity<RMCFlamerAmmoProviderComponent> ent, ref TakeAmmoEvent args)
    {
        args.Ammo.Add((ent, ent.Comp));
    }

    private void OnGetAmmoCount(Entity<RMCFlamerAmmoProviderComponent> ent, ref GetAmmoCountEvent args)
    {
        if (!TryGetTankSolution(ent, out var solutionEnt, out var _))
            return;

        var solution = solutionEnt.Value.Comp.Solution;
        args.Count = solution.Volume.Int();
        args.Capacity = solution.MaxVolume.Int();
    }

    private void OnInsertedIntoContainer(Entity<RMCFlamerAmmoProviderComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID != ent.Comp.ContainerId)
            return;

        UpdateAppearance(ent);
    }

    private void OnRemovedFromContainer(Entity<RMCFlamerAmmoProviderComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID != ent.Comp.ContainerId)
            return;

        UpdateAppearance(ent);
    }

    private void OnAttemptShoot(Entity<RMCFlamerAmmoProviderComponent> ent, ref AttemptShootEvent args)
    {
        if (args.ToCoordinates is not { } toCoordinates ||
            CanShootFlamer(ent, args.FromCoordinates, toCoordinates, out _, out var solution, out _, out _))
        {
            return;
        }

        args.Cancelled = true;
        args.ResetCooldown = true;

        var time = _timing.CurTime;
        if (time < ent.Comp.CantShootPopupLast + ent.Comp.CantShootPopupCooldown)
            return;

        ent.Comp.CantShootPopupLast = time;
        Dirty(ent);

        if (solution is not { } sol || sol.Comp.Solution.Volume < ent.Comp.CostPer)
        {
            args.Message = Loc.GetString("rmc-flamer-empty");
            return;
        }

        args.Message = Loc.GetString("rmc-flamer-too-close");
    }

    private void OnFlamerTankBeforeRangedInteract(Entity<RMCFlamerTankComponent> tank, ref BeforeRangedInteractEvent args)
    {
        if (!HasComp<RMCFlamerAmmoProviderComponent>(tank))
        {
            RefillTank(tank, ref args);
            return;
        }

        if (args.Target is not { } target)
            return;

        if (!_solution.TryGetSolution(tank.Owner, tank.Comp.SolutionId, out var tankSolutionEnt, out _))
            return;

        Entity<SolutionComponent> targetSolutionEnt;
        if (_solution.TryGetDrainableSolution(target, out var drainable, out _))
        {
            targetSolutionEnt = drainable.Value;
        }
        else if (TryComp(target, out RMCFlamerTankComponent? targetTank) &&
                 _solution.TryGetSolution(target, targetTank.SolutionId, out var targetTankSolution))
        {
            targetSolutionEnt = targetTankSolution.Value;
        }
        else if (TryComp(target, out RMCFlamerBackpackComponent? backpack) &&
                 _solution.TryGetSolution(target, backpack.SolutionId, out var backpackSolution))
        {
            targetSolutionEnt = backpackSolution.Value;
        }
        else if (HasComp<ReagentTankComponent>(target) &&
                 _solution.TryGetDrainableSolution(target, out var reagentTankSolutionEnt, out _))
        {
            targetSolutionEnt = reagentTankSolutionEnt.Value;
        }
        else
        {
            return;
        }

        args.Handled = true;
        Transfer(target, targetSolutionEnt, tank, tankSolutionEnt.Value, args.User);
    }

    private void OnFlamerTankVerbExamine(Entity<RMCFlamerTankComponent> tank, ref GetVerbsEvent<ExamineVerb> args)
    {
        var user = args.User;

        if (!args.CanInteract || !args.CanAccess || HasComp<XenoComponent>(user))
            return;

        var msg = new FormattedMessage();
        List<int> values = new([tank.Comp.MaxIntensity, tank.Comp.MaxDuration, tank.Comp.MaxRange]);
        for (var i = 0; i < values.Count; i++)
        {
            msg.AddMarkupPermissive(Loc.GetString("rmc-flamer-tank-examine-line-" + i, ("value", values[i])));

            if (i + 1 != values.Count)
                msg.PushNewline();
        }

        _examine.AddDetailedExamineVerb(args,
            tank,
            msg,
            Loc.GetString("rmc-flamer-tank-examine-short"),
            tank.Comp.ExamineIcon,
            Loc.GetString("rmc-flamer-tank-examine")
        );
    }

    private void OnSprayTakeAmmo(Entity<RMCSprayAmmoProviderComponent> ent, ref TakeAmmoEvent args)
    {
        args.Ammo.Add((ent, ent.Comp));
    }

    private void OnSprayGetAmmoCount(Entity<RMCSprayAmmoProviderComponent> ent, ref GetAmmoCountEvent args)
    {
        if (!_solution.TryGetSolution(ent.Owner, ent.Comp.SolutionId, out var solutionEnt, out _))
            return;

        var solution = solutionEnt.Value.Comp.Solution;
        args.Count = solution.Volume.Int();
        args.Capacity = solution.MaxVolume.Int();
    }

    private void OnIgniterMapInit(Entity<RMCIgniterComponent> ent, ref MapInitEvent args)
    {
        _appearance.SetData(ent, RMCIgniterVisuals.Ignited, ent.Comp.Enabled);
    }

    private void OnIgniterUniqueAction(Entity<RMCIgniterComponent> ent, ref UniqueActionEvent args)
    {
        if (args.Handled || ent.Comp.Locked)
            return;

        args.Handled = true;
        ent.Comp.Enabled = !ent.Comp.Enabled;
        Dirty(ent);

        _audio.PlayPredicted(ent.Comp.Sound, ent, args.UserUid);
        _appearance.SetData(ent, RMCIgniterVisuals.Ignited, ent.Comp.Enabled);
    }

    private void OnIgniterToggle(Entity<RMCIgniterComponent> ent, ref IsHotEvent args)
    {
        args.IsHot = ent.Comp.Enabled;
    }

    protected virtual void OnIgniterAttemptShoot(Entity<RMCIgniterComponent> ent, ref AttemptShootEvent args)
    {
        if (args.Cancelled)
            return;

        if (!ent.Comp.Enabled)
            args.Cancelled = true;
    }

    private void OnIgniterUniqueActionExamine(Entity<RMCIgniterComponent> ent, ref ExaminedEvent args)
    {
        if (ent.Comp.Locked)
            return;

        args.PushMarkup(Loc.GetString(ent.Comp.ExamineText), 1);
    }

    private void UpdateAppearance(Entity<RMCFlamerAmmoProviderComponent> ent)
    {
        if (!TryComp(ent, out AppearanceComponent? appearance))
            return;

        var volume = FixedPoint2.Zero;
        var maxVolume = FixedPoint2.Zero;
        var tank = false;
        if (TryGetTankSolution(ent, out var solutionEnt, out var _, display: true))
        {
            var solution = solutionEnt.Value.Comp.Solution;
            volume = solution.Volume;
            maxVolume = solution.MaxVolume;
            tank = true;
        }

        _appearance.SetData(ent, AmmoVisuals.HasAmmo, volume > FixedPoint2.Zero, appearance);
        _appearance.SetData(ent, AmmoVisuals.AmmoCount, volume.Int(), appearance);
        _appearance.SetData(ent, AmmoVisuals.AmmoMax, maxVolume.Int(), appearance);
        _appearance.SetData(ent, AmmoVisuals.MagLoaded, tank, appearance);
        _appearance.SetData(ent, RMCFlamerVisualLayers.Strip, tank, appearance);
    }

    public void ShootFlamer(Entity<RMCFlamerAmmoProviderComponent> flamer,
        Entity<GunComponent> gun,
        EntityUid? user,
        EntityCoordinates fromCoordinates,
        EntityCoordinates toCoordinates)
    {
        if (!CanShootFlamer(flamer, fromCoordinates, toCoordinates, out var tiles, out var solution, out var reagent, out var tank))
            return;

        _audio.PlayPredicted(gun.Comp.SoundGunshotModified, gun, user);

        //  333456
        // 1233456
        //  333456
        var cost = tiles.Count;
        if (reagent.FireSpread && cost > 2)
            cost = (int)Math.Ceiling(cost / 3.0f);

        solution.Value.Comp.Solution.RemoveSolution(flamer.Comp.CostPer * cost);
        _solution.UpdateChemicals(solution.Value);

        if (_net.IsClient)
            return;

        var chain = Spawn();
        var chainComp = EnsureComp<RMCFlamerChainComponent>(chain);
        chainComp.Spawn = reagent.FireEntity;
        chainComp.Tiles = tiles;
        chainComp.Reagent = reagent.ID;
        chainComp.MaxIntensity = tank.Value.Comp.MaxIntensity;
        chainComp.MaxDuration = tank.Value.Comp.MaxDuration;

        Dirty(chain, chainComp);
    }

    public bool TryGetPreviewTiles(
        Entity<RMCFlamerAmmoProviderComponent> flamer,
        EntityCoordinates fromCoordinates,
        EntityCoordinates toCoordinates,
        [NotNullWhen(true)] out List<LineTile>? tiles)
    {
        return CanShootFlamer(flamer, fromCoordinates, toCoordinates, out tiles, out _, out _, out _);
    }

    public bool TryGetFuelColor(Entity<RMCFlamerAmmoProviderComponent> flamer, out Color color)
    {
        color = default;
        if (!TryGetTankSolution(flamer, out var solutionEnt, out _))
            return false;

        color = solutionEnt.Value.Comp.Solution.GetColor(_prototypes);
        return true;
    }

    private bool CanShootFlamer(
        Entity<RMCFlamerAmmoProviderComponent> flamer,
        EntityCoordinates fromCoordinates,
        EntityCoordinates toCoordinates,
        [NotNullWhen(true)] out List<LineTile>? tiles,
        [NotNullWhen(true)] out Entity<SolutionComponent>? solution,
        [NotNullWhen(true)] out ReagentPrototype? reagent,
        [NotNullWhen(true)] out Entity<RMCFlamerTankComponent>? tank)
    {
        tiles = null;
        reagent = null;
        if (!TryGetTankSolution(flamer, out solution, out tank))
            return false;

        var volume = solution.Value.Comp.Solution.Volume;
        if (volume < flamer.Comp.CostPer)
            return false;

        if (!fromCoordinates.TryDelta(EntityManager, _transform, toCoordinates, out var delta))
            return false;

        if (delta.IsLengthZero())
            return false;

        var normalized = -delta.Normalized();

        // to prevent hitting yourself
        fromCoordinates = fromCoordinates.Offset(normalized * 0.23f);

        if (!solution.Value.Comp.Solution.TryFirstOrNull(out var firstReagent))
            return false;

        reagent = _reagent.Index(firstReagent.Value.Reagent.Prototype);

        var maxRange = Math.Min(tank.Value.Comp.MaxRange, reagent.Radius);
        var range = Math.Min((volume / flamer.Comp.CostPer).Int(), maxRange);
        if (delta.Length() > maxRange)
            toCoordinates = fromCoordinates.Offset(normalized * range);

        tiles = _line.DrawLine(fromCoordinates, toCoordinates, flamer.Comp.DelayPer, maxRange, out _, true, reagent.FireSpread);
        if (tiles.Count == 0)
        {
            tiles = null;
            return false;
        }

        return true;
    }

    public void ShootSpray(Entity<RMCSprayAmmoProviderComponent> spray,
        Entity<GunComponent> gun,
        EntityUid? user,
        EntityCoordinates fromCoordinates,
        EntityCoordinates toCoordinates)
    {
        if (user == null)
            return;

        _rmcSpray.Spray(spray, user.Value, _transform.ToMapCoordinates(toCoordinates), spray.Comp.HitUser);
    }

    /// <summary>
    /// Get the solution that will be used by the flamer
    /// </summary>
    /// <param name="flamer">The incinerator that is being used.</param>
    /// <param name="solutionEnt">The found solution.</param>
    /// <param name="tankEnt">The found tank.</param>
    /// <param name="display">Is this just being called to configure the sprite? It ignores the Broiler if true.</param>
    /// <returns>True if a solution has been found.</returns>
    private bool TryGetTankSolution(Entity<RMCFlamerAmmoProviderComponent> flamer, [NotNullWhen(true)] out Entity<SolutionComponent>? solutionEnt, [NotNullWhen(true)] out Entity<RMCFlamerTankComponent>? tankEnt, bool display = false)
    {
        solutionEnt = null;
        tankEnt = null;

        if (TryComp(flamer, out RMCFlamerTankComponent? tankComp))
        {
            tankEnt = (flamer, tankComp);
        }
        else if (_container.TryGetContainer(flamer, flamer.Comp.ContainerId, out var container) &&
                 container.ContainedEntities.TryFirstOrNull(out var tankId) &&
                 TryComp(tankId, out tankComp))
        {
            tankEnt = (tankId.Value, tankComp);
        }
        else if (!display && HasComp<RMCCanUseBroilerComponent>(flamer))
        {
            if (!_container.TryGetContainingContainer((flamer.Owner, null), out var holder))
                return false;

            var inventoryEnumerator = _inventory.GetSlotEnumerator(holder.Owner);
            while (inventoryEnumerator.MoveNext(out var slot))
            {
                if (!TryComp<RMCBroilerComponent>(slot.ContainedEntity, out var broiler))
                    continue;

                Entity<RMCBroilerComponent> broilerEnt = (slot.ContainedEntity.Value, broiler);
                var containers = BroilerListTanks(broilerEnt);
                if (containers.Count <= broiler.ActiveTank)
                    continue;

                var activeTankContainerName = containers[broiler.ActiveTank];
                if (!_container.TryGetContainer(broilerEnt, activeTankContainerName, out var activeTankContainer))
                    continue;

                if (!activeTankContainer.ContainedEntities.TryFirstOrNull(out tankId))
                    continue;

                if (!TryComp(tankId, out tankComp))
                    continue;

                tankEnt = (tankId.Value, tankComp);
                break;
            }
        }

        if (tankEnt is not { } tankValue)
            return false;

        return _solution.TryGetSolution(tankValue.Owner, tankValue.Comp.SolutionId, out solutionEnt, out _);
    }

    private void Transfer(EntityUid source,
        Entity<SolutionComponent> sourceSolutionEnt,
        Entity<RMCFlamerTankComponent> target,
        Entity<SolutionComponent> targetSolutionEnt,
        EntityUid user)
    {
        var tankSolution = targetSolutionEnt.Comp.Solution;
        var targetSolution = sourceSolutionEnt.Comp.Solution;
        foreach (var content in targetSolution.Contents)
        {
            if (target.Comp.ReagentWhitelist is { } whitelist && !whitelist.Contains(content.Reagent.Prototype))
            {
                _popup.PopupClient(Loc.GetString("rmc-flamer-tank-not-whitelisted", ("tank", target)), source, user);
                return;
            }
            if (_reagent.TryIndex(content.Reagent.Prototype, out var reagent) &&
                (reagent.Intensity <= 0 || reagent.Duration <= 0 || reagent.Radius <= 0))
            {
                _popup.PopupClient(Loc.GetString("rmc-flamer-tank-not-potent-enough"), source, user);
                return;
            }
        }

        var transfer = _solutionTransfer.Transfer(
            user,
            source,
            sourceSolutionEnt,
            target,
            targetSolutionEnt,
            tankSolution.AvailableVolume
        );

        if (transfer > FixedPoint2.Zero)
            _popup.PopupClient(Loc.GetString("rmc-flamer-refill", ("refilled", target)), source, user);
    }

    private void RefillTank(Entity<RMCFlamerTankComponent> tank, ref BeforeRangedInteractEvent args)
    {
        if (args.Target is not { } target)
            return;

        if (!_solution.TryGetSolution(tank.Owner, tank.Comp.SolutionId, out var tankSolutionEnt, out _))
            return;

        Entity<SolutionComponent> targetSolutionEnt;
        if (HasComp<ReagentTankComponent>(target) &&
            _solution.TryGetDrainableSolution(target, out var reagentTankSolutionEnt, out _))
        {
            targetSolutionEnt = reagentTankSolutionEnt.Value;
        }
        else
        {
            return;
        }

        args.Handled = true;
        Transfer(target, targetSolutionEnt, tank, tankSolutionEnt.Value, args.User);
    }

    private void OnBroilerGetItemActions(Entity<RMCBroilerComponent> ent, ref GetItemActionsEvent args)
    {
        if (args.SlotFlags == null || (args.SlotFlags & ent.Comp.Slot) == 0)
            return;

        args.AddAction(ref ent.Comp.Action, ent.Comp.ActionId, ent);
        if (ent.Comp.Action is { } action)
        {
            var n = ent.Comp.ActiveTank + 1;
            _action.SetIcon(action, new SpriteSpecifier.Rsi(ent.Comp.NumberingResource, n.ToString()));
        }
    }

    private List<string> BroilerListTanks(Entity<RMCBroilerComponent> ent)
    {
        List<string> list = [];
        foreach (var container in _container.GetAllContainers(ent))
        {
            var name = container.ID;
            if (name.StartsWith(ent.Comp.ContainerPrefix))
                list.Add(name);
        }
        return list;
    }

    private void OnBroilerAction(Entity<RMCBroilerComponent> ent, ref RMCBroilerActionEvent args)
    {
        args.Handled = true;

        ent.Comp.ActiveTank = (ent.Comp.ActiveTank + 1) % BroilerListTanks(ent).Count;
        Dirty(ent);

        var n = ent.Comp.ActiveTank + 1;
        if (ent.Comp.Action is { } action)
        {
            _action.SetIcon(action, new SpriteSpecifier.Rsi(ent.Comp.NumberingResource, n.ToString()));
        }

        _popup.PopupClient(Loc.GetString("rmc-broiler-switch-tank", ("n", n)), ent, args.Performer);
    }

    public void OnBroilerUniqueAction(Entity<RMCCanUseBroilerComponent> ent, ref UniqueActionEvent args)
    {
        if (args.Handled)
            return;

        var inventoryEnumerator = _inventory.GetSlotEnumerator(args.UserUid);
        while (inventoryEnumerator.MoveNext(out var slot))
        {
            if (!TryComp<RMCBroilerComponent>(slot.ContainedEntity, out var _))
                continue;

            args.Handled = true;
            var ev = new RMCBroilerActionEvent();
            ev.Performer = args.UserUid;
            RaiseLocalEvent(slot.ContainedEntity.Value, ev);
            break;
        }
    }

    public void OnBroilerUniqueActionExamine(Entity<RMCCanUseBroilerComponent> ent, ref ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString(ent.Comp.ExamineText), 1);
    }

    public override void Update(float frameTime)
    {
        if (_net.IsClient)
            return;

        var time = _timing.CurTime;
        var chains = EntityQueryEnumerator<RMCFlamerChainComponent>();
        while (chains.MoveNext(out var uid, out var comp))
        {
            if (comp.Tiles.Count == 0)
            {
                QueueDel(uid);
                continue;
            }

            foreach (var tile in comp.Tiles)
            {
                if (time >= tile.At)
                {
                    comp.Tiles.Remove(tile);
                    var fire = Spawn(comp.Spawn, tile.Coordinates);

                    // check for any fires on the same tile other than the one we just spawned, and delete them
                    if (_rmcMap.HasAnchoredEntityEnumerator<TileFireComponent>(_transform.ToCoordinates(fire, tile.Coordinates), out var oldTileFire)
                        && oldTileFire.Owner.Id != fire.Id)
                    {
                        QueueDel(oldTileFire);
                    }

                    if (_reagent.TryIndex(comp.Reagent, out var reagent))
                    {
                        var intensity = Math.Min(comp.MaxIntensity, reagent.Intensity);
                        var duration = Math.Min(comp.MaxDuration, reagent.Duration);
                        _rmcFlammable.SetIntensityDuration(fire, intensity, duration);
                    }

                    break;
                }
            }
        }
    }
}
