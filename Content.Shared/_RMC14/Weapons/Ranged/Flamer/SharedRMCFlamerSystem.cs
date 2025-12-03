using System.Diagnostics.CodeAnalysis;
using Content.Shared._RMC14.Atmos;
using Content.Shared._RMC14.Chemistry.Reagent;
using Content.Shared._RMC14.Fluids;
using Content.Shared._RMC14.Line;
using Content.Shared._RMC14.Map;
using Content.Shared._RMC14.Weapons.Common;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Temperature;
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
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly LineSystem _line = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly SharedRMCFlammableSystem _rmcFlammable = default!;
    [Dependency] private readonly SharedRMCSpraySystem _rmcSpray = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;
    [Dependency] private readonly SolutionTransferSystem _solutionTransfer = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly RMCMapSystem _rmcMap = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCFlamerAmmoProviderComponent, MapInitEvent>(OnMapInit, after: [typeof(SharedSolutionContainerSystem)]);
        SubscribeLocalEvent<RMCFlamerAmmoProviderComponent, TakeAmmoEvent>(OnTakeAmmo);
        SubscribeLocalEvent<RMCFlamerAmmoProviderComponent, GetAmmoCountEvent>(OnGetAmmoCount);
        SubscribeLocalEvent<RMCFlamerAmmoProviderComponent, EntInsertedIntoContainerMessage>(OnInsertedIntoContainer);
        SubscribeLocalEvent<RMCFlamerAmmoProviderComponent, EntRemovedFromContainerMessage>(OnRemovedFromContainer);
        SubscribeLocalEvent<RMCFlamerAmmoProviderComponent, AttemptShootEvent>(OnAttemptShoot);

        SubscribeLocalEvent<RMCFlamerTankComponent, BeforeRangedInteractEvent>(OnFlamerTankBeforeRangedInteract);

        SubscribeLocalEvent<RMCSprayAmmoProviderComponent, TakeAmmoEvent>(OnSprayTakeAmmo);
        SubscribeLocalEvent<RMCSprayAmmoProviderComponent, GetAmmoCountEvent>(OnSprayGetAmmoCount);

        SubscribeLocalEvent<RMCIgniterComponent, MapInitEvent>(OnIgniterMapInit, after: [typeof(SharedSolutionContainerSystem)]);
        SubscribeLocalEvent<RMCIgniterComponent, UniqueActionEvent>(OnIgniterUniqueAction);
        SubscribeLocalEvent<RMCIgniterComponent, IsHotEvent>(OnIgniterToggle);
        SubscribeLocalEvent<RMCIgniterComponent, AttemptShootEvent>(OnIgniterAttemptShoot);
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
        if (!TryGetTankSolution(ent, out var solutionEnt))
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
            CanShootFlamer(ent, args.FromCoordinates, toCoordinates, out _, out _))
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

    private void UpdateAppearance(Entity<RMCFlamerAmmoProviderComponent> ent)
    {
        if (!TryComp(ent, out AppearanceComponent? appearance))
            return;

        var volume = FixedPoint2.Zero;
        var maxVolume = FixedPoint2.Zero;
        var tank = false;
        if (TryGetTankSolution(ent, out var solutionEnt))
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
        if (!CanShootFlamer(flamer, fromCoordinates, toCoordinates, out var tiles, out var solution))
            return;

        _audio.PlayPredicted(gun.Comp.SoundGunshotModified, gun, user);

        ProtoId<ReagentPrototype>? reagent = null;
        if (solution.Comp.Solution.TryFirstOrNull(out var firstReagent))
            reagent = firstReagent.Value.Reagent.Prototype;

        solution.Comp.Solution.RemoveSolution(flamer.Comp.CostPer * tiles.Count);
        _solution.UpdateChemicals(solution);

        if (_net.IsClient)
            return;

        var chain = Spawn();
        var chainComp = EnsureComp<RMCFlamerChainComponent>(chain);
        chainComp.Spawn = flamer.Comp.Spawn;
        chainComp.Tiles = tiles;
        chainComp.MaxIntensity = flamer.Comp.MaxIntensity;
        chainComp.MaxDuration = flamer.Comp.MaxDuration;

        if (reagent != null)
            chainComp.Reagent = reagent.Value;

        Dirty(chain, chainComp);
    }

    private bool CanShootFlamer(
        Entity<RMCFlamerAmmoProviderComponent> flamer,
        EntityCoordinates fromCoordinates,
        EntityCoordinates toCoordinates,
        [NotNullWhen(true)] out List<LineTile>? tiles,
        out Entity<SolutionComponent> solution)
    {
        tiles = null;
        solution = default;
        if (!TryGetTankSolution(flamer, out var solutionEnt))
            return false;

        var volume = solutionEnt.Value.Comp.Solution.Volume;
        if (volume <= flamer.Comp.CostPer)
            return false;

        if (!fromCoordinates.TryDelta(EntityManager, _transform, toCoordinates, out var delta))
            return false;

        if (delta.IsLengthZero())
            return false;

        var normalized = -delta.Normalized();

        // to prevent hitting yourself
        fromCoordinates = fromCoordinates.Offset(normalized * 0.23f);

        var range = Math.Min((volume / flamer.Comp.CostPer).Int(), flamer.Comp.Range);
        if (delta.Length() > flamer.Comp.Range)
            toCoordinates = fromCoordinates.Offset(normalized * range);

        tiles = _line.DrawLine(fromCoordinates, toCoordinates, flamer.Comp.DelayPer, flamer.Comp.Range, out _, true);
        if (tiles.Count == 0)
        {
            tiles = null;
            return false;
        }

        solution = solutionEnt.Value;
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

        _rmcSpray.Spray(spray, user.Value, _transform.ToMapCoordinates(toCoordinates));
    }

    private bool TryGetTankSolution(Entity<RMCFlamerAmmoProviderComponent> flamer, [NotNullWhen(true)] out Entity<SolutionComponent>? solutionEnt)
    {
        solutionEnt = null;

        Entity<RMCFlamerTankComponent> tank;
        if (TryComp(flamer, out RMCFlamerTankComponent? tankComp))
        {
            tank = (flamer, tankComp);
        }
        else if (_container.TryGetContainer(flamer, flamer.Comp.ContainerId, out var container) &&
                 container.ContainedEntities.TryFirstOrNull(out var tankId) &&
                 TryComp(tankId, out tankComp))
        {
            tank = (tankId.Value, tankComp);
        }
        else
        {
            return false;
        }

        return _solution.TryGetSolution(tank.Owner, tank.Comp.SolutionId, out solutionEnt, out _);
    }

    private void Transfer(EntityUid source,
        Entity<SolutionComponent> sourceSolutionEnt,
        EntityUid target,
        Entity<SolutionComponent> targetSolutionEnt,
        EntityUid user)
    {
        var tankSolution = targetSolutionEnt.Comp.Solution;
        var targetSolution = sourceSolutionEnt.Comp.Solution;
        foreach (var content in targetSolution.Contents)
        {
            if (_prototypes.TryIndexReagent(content.Reagent.Prototype, out ReagentPrototype? reagent) &&
                reagent.Intensity <= FixedPoint2.Zero)
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

                    if (_prototypes.TryIndexReagent(comp.Reagent, out var reagent))
                    {
                        var intensity = Math.Min(comp.MaxIntensity, reagent.Intensity.Int());
                        var duration = Math.Min(comp.MaxDuration, reagent.Duration.Int());
                        _rmcFlammable.SetIntensityDuration(fire, intensity, duration);
                    }

                    break;
                }
            }
        }
    }
}
