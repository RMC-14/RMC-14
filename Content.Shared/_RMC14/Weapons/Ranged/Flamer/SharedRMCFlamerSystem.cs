using System.Diagnostics.CodeAnalysis;
using Content.Shared._RMC14.Atmos;
using Content.Shared._RMC14.Vehicle;
using Content.Shared._RMC14.Attachable.Components;
using Content.Shared._RMC14.Chemistry.Effects;
using Content.Shared._RMC14.Chemistry.Reagent;
using Content.Shared._RMC14.Fluids;
using Content.Shared._RMC14.Line;
using Content.Shared._RMC14.Map;
using Content.Shared._RMC14.OnCollide;
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
    [Dependency] private readonly SharedOnCollideSystem _onCollide = default!;
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

    private static readonly EntProtoId SmokeChemical = "RMCSmokeChemical";

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCFlamerAmmoProviderComponent, MapInitEvent>(OnMapInit, after: [typeof(SharedSolutionContainerSystem)]);
        SubscribeLocalEvent<RMCFlamerAmmoProviderComponent, TakeAmmoEvent>(OnTakeAmmo);
        SubscribeLocalEvent<RMCFlamerAmmoProviderComponent, GetAmmoCountEvent>(OnGetAmmoCount);
        SubscribeLocalEvent<RMCFlamerAmmoProviderComponent, EntInsertedIntoContainerMessage>(OnInsertedIntoContainer);
        SubscribeLocalEvent<RMCFlamerAmmoProviderComponent, EntRemovedFromContainerMessage>(OnRemovedFromContainer);
        SubscribeLocalEvent<RMCFlamerAmmoProviderComponent, AttemptShootEvent>(OnAttemptShoot);
        SubscribeLocalEvent<RMCFlamerAmmoProviderComponent, ExaminedEvent>(OnFlamerExamine, before: [typeof(SharedGunSystem)]);

        SubscribeLocalEvent<RMCFlamerTankComponent, BeforeRangedInteractEvent>(OnFlamerTankBeforeRangedInteract);
        SubscribeLocalEvent<RMCFlamerTankComponent, GetVerbsEvent<ExamineVerb>>(OnFlamerTankVerbExamine);
        SubscribeLocalEvent<RMCFlamerTankComponent, GetVerbsEvent<AlternativeVerb>>(OnFlamerTankGetAltVerbs);

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
        SubscribeLocalEvent<RMCFlamerChainComponent, ComponentShutdown>(OnFlamerChainShutdown);
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
            CanShootFlamer(ent, args.FromCoordinates, toCoordinates, out _, out var solution, out _, out _, out _))
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
        if (!args.CanReach)
            return;

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

    private void OnFlamerTankGetAltVerbs(Entity<RMCFlamerTankComponent> tank, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!tank.Comp.AdjustablePressure || !args.CanInteract || !args.CanAccess || HasComp<XenoComponent>(args.User))
            return;

        var user = args.User;
        args.Verbs.Add(new AlternativeVerb
        {
            Text = Loc.GetString("rmc-flamer-tank-pressure-verb"),
            Act = () => CycleFuelPressure(tank, user),
        });
    }

    private void CycleFuelPressure(Entity<RMCFlamerTankComponent> tank, EntityUid user)
    {
        var next = tank.Comp.FuelPressure + 1;
        if (next > tank.Comp.MaxPressure)
            next = 1;

        tank.Comp.FuelPressure = next;
        Dirty(tank);
        _popup.PopupClient(Loc.GetString("rmc-flamer-tank-pressure-set", ("pressure", next)), tank, user);
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

        if (tank.Comp.AdjustablePressure)
        {
            msg.PushNewline();
            msg.AddMarkupPermissive(Loc.GetString("rmc-flamer-tank-examine-pressure", ("value", tank.Comp.FuelPressure)));
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
        if (TryComp<AttachableHolderComponent>(ent, out var holder) &&
            holder.SupercedingAttachable != null)
        {
            args.IsHot = false;
            return;
        }

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

    private void OnFlamerExamine(Entity<RMCFlamerAmmoProviderComponent> ent, ref ExaminedEvent args)
    {
        if (!TryGetTankSolution(ent, out _, out var tank) || !tank.Value.Comp.AdjustablePressure)
            return;

        args.PushMarkup(Loc.GetString("rmc-flamer-loaded-pressure", ("value", tank.Value.Comp.FuelPressure)));
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

    private FixedPoint2 GetCostPer(Entity<RMCFlamerAmmoProviderComponent> flamer, Entity<RMCFlamerTankComponent> tank)
    {
        return tank.Comp.AdjustablePressure || tank.Comp.Smoke
            ? FixedPoint2.New(tank.Comp.FuelPressure)
            : flamer.Comp.CostPer;
    }

    public void ShootFlamer(Entity<RMCFlamerAmmoProviderComponent> flamer,
        Entity<GunComponent> gun,
        EntityUid? user,
        EntityCoordinates fromCoordinates,
        EntityCoordinates toCoordinates)
    {
        if (!CanShootFlamer(flamer, fromCoordinates, toCoordinates, out var tiles, out var solution, out var reagent, out var reagentId, out var tank))
            return;

        _audio.PlayPredicted(gun.Comp.SoundGunshotModified, gun, user);

        //  333456
        // 1233456
        //  333456
        var cost = tiles.Count;
        if (!tank.Value.Comp.Smoke && reagent.FireSpread && cost > 2)
            cost = (int)Math.Ceiling(cost / 3.0f);

        var costPer = GetCostPer(flamer, tank.Value);
        solution.Value.Comp.Solution.RemoveReagent(reagentId, costPer * cost);
        _solution.UpdateChemicals(solution.Value);

        if (_net.IsClient)
            return;

        var chain = Spawn();
        var chainComp = EnsureComp<RMCFlamerChainComponent>(chain);
        chainComp.Tiles = tiles;
        chainComp.Reagent = reagent.ID;
        chainComp.FuelPressure = (int)costPer;

        if (tank.Value.Comp.Smoke)
        {
            chainComp.Smoke = true;
            chainComp.Spawn = SmokeChemical;
        }
        else
        {
            chainComp.Spawn = reagent.FireEntity;
            chainComp.MinIntensity = tank.Value.Comp.MinIntensity;
            chainComp.MaxIntensity = tank.Value.Comp.MaxIntensity;
            chainComp.MinDuration = tank.Value.Comp.MinDuration;
            chainComp.MaxDuration = tank.Value.Comp.MaxDuration;
            chainComp.FirePenetrating = GetFireStats(reagent).FirePenetrating;
        }

        Dirty(chain, chainComp);
    }

    public bool TryGetPreviewTiles(
        Entity<RMCFlamerAmmoProviderComponent> flamer,
        EntityCoordinates fromCoordinates,
        EntityCoordinates toCoordinates,
        [NotNullWhen(true)] out List<LineTile>? tiles)
    {
        return CanShootFlamer(flamer, fromCoordinates, toCoordinates, out tiles, out _, out _, out _, out _);
    }

    public bool TryGetFuelColor(Entity<RMCFlamerAmmoProviderComponent> flamer, out Color color)
    {
        color = default;
        if (!TryGetTankSolution(flamer, out var solutionEnt, out _))
            return false;

        color = solutionEnt.Value.Comp.Solution.GetColor(_prototypes);
        return true;
    }

    public RMCFireStats GetFireStats(ReagentPrototype reagent, float baseIntensity = 0, float baseDuration = 0)
    {
        var stats = new RMCFireStatsBuilder
        {
            Intensity = baseIntensity + reagent.Intensity,
            Duration = baseDuration + reagent.Duration,
            Radius = reagent.Radius,
            IntensityMod = reagent.IntensityMod.Float(),
            DurationMod = reagent.DurationMod.Float(),
            RadiusMod = reagent.RadiusMod.Float(),
            FirePenetrating = reagent.FirePenetrating,
        };

        var fireSupplement = reagent.IntensityMod != FixedPoint2.Zero ||
                             reagent.DurationMod != FixedPoint2.Zero ||
                             reagent.RadiusMod != FixedPoint2.Zero ||
                             reagent.FirePenetrating;

        foreach (var effect in _reagent.GetProperties(reagent.ID))
        {
            effect.ModifyFireStats(ref stats, effect.Potency);
            fireSupplement |= effect.IsFireSupplement;
        }

        if (stats.HasFireProperty)
        {
            stats.Intensity = MathF.Max(stats.Intensity, 1f);
            stats.Duration = MathF.Max(stats.Duration, 1f);
            stats.Radius = MathF.Max(stats.Radius, 1f);
        }

        return new RMCFireStats(
            (int) stats.Intensity,
            (int) stats.Duration,
            (int) stats.Radius,
            stats.IntensityMod,
            stats.DurationMod,
            stats.RadiusMod,
            stats.FirePenetrating,
            fireSupplement,
            stats.HasFireProperty);
    }

    private const int FirePenetrationThreshold = 10;

    public RMCAggregateFireStats GetAggregateFireStats(Solution solution)
    {
        var intensity = 0f;
        var duration = 0f;
        var radius = 0f;
        var firePenetrating = false;

        var totalWeight = 0f;
        var red = 0f;
        var green = 0f;
        var blue = 0f;

        foreach (var quantity in solution.Contents)
        {
            if (!_reagent.TryIndex(quantity.Reagent.Prototype, out var reagent))
                continue;

            var stats = GetFireStats(reagent);

            if (!stats.FireSupplement)
                continue;

            var volume = quantity.Quantity.Float();
            intensity += stats.IntensityMod * volume;
            duration += stats.DurationMod * volume;
            radius += stats.RadiusMod * volume;
            firePenetrating |= stats.FirePenetrating && quantity.Quantity >= FirePenetrationThreshold;

            var burnColor = _rmcFlammable.GetBurnColor(reagent, stats);
            var weight = MathF.Max(volume, 1f) * reagent.BurnColorMod;
            totalWeight += weight;
            red += weight * burnColor.RByte;
            green += weight * burnColor.GByte;
            blue += weight * burnColor.BByte;
        }

        Color? fireColor = null;
        if (totalWeight > 0)
        {
            fireColor = new Color(
                MixColorChannel(red, totalWeight),
                MixColorChannel(green, totalWeight),
                MixColorChannel(blue, totalWeight));
        }

        return new RMCAggregateFireStats(
            FixedPoint2.New(intensity),
            FixedPoint2.New(duration),
            FixedPoint2.New(radius),
            firePenetrating,
            fireColor);
    }

    private static byte MixColorChannel(float weightedSum, float totalWeight)
    {
        return (byte) Math.Clamp(MathF.Floor(weightedSum / totalWeight), 0, 255);
    }

    private bool CanShootFlamer(
        Entity<RMCFlamerAmmoProviderComponent> flamer,
        EntityCoordinates fromCoordinates,
        EntityCoordinates toCoordinates,
        [NotNullWhen(true)] out List<LineTile>? tiles,
        [NotNullWhen(true)] out Entity<SolutionComponent>? solution,
        [NotNullWhen(true)] out ReagentPrototype? reagent,
        out ReagentId reagentId,
        [NotNullWhen(true)] out Entity<RMCFlamerTankComponent>? tank)
    {
        tiles = null;
        reagent = null;
        reagentId = default;
        if (!TryGetTankSolution(flamer, out solution, out tank))
            return false;

        if (!solution.Value.Comp.Solution.TryFirstOrNull(out var firstReagent))
            return false;

        var volume = firstReagent.Value.Quantity;
        var costPer = GetCostPer(flamer, tank.Value);
        if (volume < costPer)
            return false;

        if (!fromCoordinates.TryDelta(EntityManager, _transform, toCoordinates, out var delta))
            return false;

        if (delta.IsLengthZero())
            return false;

        var normalized = -delta.Normalized();

        // to prevent hitting yourself
        fromCoordinates = fromCoordinates.Offset(normalized * 0.23f);

        reagentId = firstReagent.Value.Reagent;
        reagent = _reagent.Index(firstReagent.Value.Reagent.Prototype);
        var fireStats = GetFireStats(reagent);

        // fixed range for smoke
        var maxRange = tank.Value.Comp.Smoke ? tank.Value.Comp.MaxRange : fireStats.Radius;
        var range = Math.Min((volume / costPer).Int(), maxRange);
        if (delta.Length() > maxRange)
            toCoordinates = fromCoordinates.Offset(normalized * range);

        var wideShape = !tank.Value.Comp.Smoke && reagent.FireSpread;
        tiles = _line.DrawLine(fromCoordinates, toCoordinates, flamer.Comp.DelayPer, maxRange, out _, true, wideShape);
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

            if (TryComp(flamer, out VehicleFlamerTankSlotsComponent? tankSlots) &&
                _solution.TryGetSolution(tankEnt.Value.Owner, tankEnt.Value.Comp.SolutionId, out var primarySol, out _) &&
                primarySol.Value.Comp.Solution.Volume < flamer.Comp.CostPer)
            {
                for (var i = 1; i < tankSlots.MaxTanks; i++)
                {
                    var extraSlotId = $"{flamer.Comp.ContainerId}_{i + 1}";
                    if (!_container.TryGetContainer(flamer, extraSlotId, out var extraContainer) ||
                        !extraContainer.ContainedEntities.TryFirstOrNull(out var extraTankId) ||
                        !TryComp(extraTankId, out RMCFlamerTankComponent? extraTankComp))
                        continue;

                    tankEnt = (extraTankId.Value, extraTankComp);
                    break;
                }
            }
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

    public void Transfer(EntityUid source,
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

            if (!target.Comp.Custom && _reagent.IsGenerated(content.Reagent.Prototype))
            {
                _popup.PopupClient(Loc.GetString("rmc-flamer-tank-no-custom-fuel", ("tank", target)), source, user);
                return;
            }

            if (_reagent.TryIndex(content.Reagent.Prototype, out var reagent))
            {
                if (!target.Comp.Specialist && reagent.Specialist)
                {
                    _popup.PopupClient(Loc.GetString("rmc-flamer-tank-no-specialist-fuel", ("tank", target)), source, user);
                    return;
                }

                var fireStats = GetFireStats(reagent);
                if (fireStats.Intensity <= 0)
                {
                    _popup.PopupClient(Loc.GetString("rmc-flamer-tank-not-potent-enough"), source, user);
                    return;
                }
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

    private void OnFlamerChainShutdown(Entity<RMCFlamerChainComponent> ent, ref ComponentShutdown args)
    {
        _onCollide.CleanupChain(ent.Comp.Chain);
    }

    public override void Update(float frameTime)
    {
        if (_net.IsClient)
            return;

        var time = _timing.CurTime;
        var chains = EntityQueryEnumerator<RMCFlamerChainComponent>();
        while (chains.MoveNext(out var uid, out var comp))
        {
            if (comp.Smoke)
                continue;

            if (comp.Tiles.Count == 0)
            {
                QueueDel(uid);
                continue;
            }
            comp.Chain ??= _onCollide.SpawnChain();

            foreach (var tile in comp.Tiles)
            {
                if (time >= tile.At)
                {
                    comp.Tiles.Remove(tile);
                    var fire = Spawn(comp.Spawn, tile.Coordinates);

                    EnsureComp<DamageOnCollideComponent>(fire, out var collide);
                    _onCollide.SetChain((fire, collide), comp.Chain.Value);

                    // check for any fires on the same tile other than the one we just spawned, and delete them
                    if (_rmcMap.HasAnchoredEntityEnumerator<TileFireComponent>(_transform.ToCoordinates(fire, tile.Coordinates), out var oldTileFire)
                        && oldTileFire.Owner.Id != fire.Id)
                    {
                        QueueDel(oldTileFire);
                    }

                    if (_reagent.TryIndex(comp.Reagent, out var reagent))
                    {
                        var fireStats = GetFireStats(reagent);
                        var intensity = Math.Clamp(fireStats.Intensity, comp.MinIntensity, comp.MaxIntensity);
                        var duration = Math.Clamp(fireStats.Duration, comp.MinDuration, comp.MaxDuration) + (int)(comp.FuelPressure * fireStats.DurationMod);

                        var fireColor = _rmcFlammable.GetFireColor(reagent);
                        _rmcFlammable.SetIntensityDuration(fire, intensity, duration, comp.FirePenetrating, fireColor);
                    }

                    break;
                }
            }
        }
    }
}

public readonly record struct RMCFireStats(
    int Intensity,
    int Duration,
    int Radius,
    float IntensityMod,
    float DurationMod,
    float RadiusMod,
    bool FirePenetrating,
    bool FireSupplement,
    bool HasFireProperty);

public readonly record struct RMCAggregateFireStats(
    FixedPoint2 Intensity,
    FixedPoint2 Duration,
    FixedPoint2 Radius,
    bool FirePenetrating,
    Color? FireColor);
